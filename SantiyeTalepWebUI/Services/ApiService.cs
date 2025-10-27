using Newtonsoft.Json;
using System.Text;
using System.Net;

namespace SantiyeTalepWebUI.Services
{
    public interface IApiService
    {
        Task<T?> GetAsync<T>(string endpoint, string? token = null);
        Task<T?> PostAsync<T>(string endpoint, object data, string? token = null);
        Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, string? token = null);
        Task<T?> PutAsync<T>(string endpoint, object data, string? token = null);
        Task<bool> DeleteAsync(string endpoint, string? token = null);
    }

    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;
        private readonly int _maxRetries = 3;
        private readonly TimeSpan _baseDelay = TimeSpan.FromSeconds(1);

        public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("SantiyeTalepAPI");
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string endpoint, string? token = null)
        {
            return await ExecuteWithRetry(async () =>
            {
                _logger.LogInformation($"Making GET request to: {endpoint}");
                
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.SendAsync(request);
                return await ProcessResponse<T>(response, endpoint, "GET");
            });
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data, string? token = null)
        {
            return await ExecuteWithRetry(async () =>
            {
                _logger.LogInformation($"Making POST request to: {endpoint}");
                
                var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss",
                    NullValueHandling = NullValueHandling.Ignore
                });
                
                _logger.LogDebug($"Request payload: {json}");

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.SendAsync(request);
                return await ProcessResponse<T>(response, endpoint, "POST", json);
            });
        }

        public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content, string? token = null)
        {
            return await ExecuteWithRetry(async () =>
            {
                _logger.LogInformation($"Making POST multipart request to: {endpoint}");

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = content
                };

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.SendAsync(request);
                return await ProcessResponse<T>(response, endpoint, "POST-MULTIPART");
            });
        }

        public async Task<T?> PutAsync<T>(string endpoint, object data, string? token = null)
        {
            return await ExecuteWithRetry(async () =>
            {
                _logger.LogInformation($"Making PUT request to: {endpoint}");
                
                var json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss",
                    NullValueHandling = NullValueHandling.Ignore
                });
                
                _logger.LogDebug($"Request payload: {json}");

                var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.SendAsync(request);
                return await ProcessResponse<T>(response, endpoint, "PUT", json);
            });
        }

        public async Task<bool> DeleteAsync(string endpoint, string? token = null)
        {
            try
            {
                var result = await ExecuteWithRetry(async () =>
                {
                    _logger.LogInformation($"Making DELETE request to: {endpoint}");
                    
                    var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"DELETE request to {endpoint} succeeded with status {response.StatusCode}");
                        return true;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"DELETE request to {endpoint} failed: {response.StatusCode} - {content}");
                    
                    // For Bad Request (400), we want to throw an exception with the specific error message
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        // Try to parse error message from response
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
                            var message = errorResponse?.message?.ToString() ?? "Bad Request";
                            throw new HttpRequestException($"400: {message}");
                        }
                        catch (JsonException)
                        {
                            throw new HttpRequestException($"400: Bad Request - {content}");
                        }
                    }
                    
                    // For other errors, also throw exceptions so the controller can handle them
                    throw new HttpRequestException($"{(int)response.StatusCode}: {response.ReasonPhrase} - {content}");
                });

                return result;
            }
            catch (HttpRequestException)
            {
                // Re-throw HttpRequestException so controller can catch and handle it
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DELETE request to {endpoint}");
                throw new HttpRequestException($"Error in DELETE request: {ex.Message}");
            }
        }

        private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation)
        {
            Exception? lastException = null;
            
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (HttpRequestException ex) when (attempt < _maxRetries)
                {
                    lastException = ex;
                    var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    _logger.LogWarning($"Attempt {attempt + 1} failed with HttpRequestException. Retrying in {delay.TotalSeconds} seconds. Error: {ex.Message}");
                    await Task.Delay(delay);
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException && attempt < _maxRetries)
                {
                    lastException = ex;
                    var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    _logger.LogWarning($"Attempt {attempt + 1} failed with timeout. Retrying in {delay.TotalSeconds} seconds.");
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Non-retryable error occurred on attempt {attempt + 1}");
                    throw;
                }
            }

            _logger.LogError($"All {_maxRetries + 1} attempts failed");
            throw lastException ?? new Exception("All retry attempts failed");
        }

        private async Task<T?> ProcessResponse<T>(HttpResponseMessage response, string endpoint, string method, string? requestPayload = null)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug($"{method} {endpoint} - Status: {response.StatusCode}, Content: {content}");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning($"Successful response but empty content for {method} {endpoint}");
                        return default;
                    }

                    var result = JsonConvert.DeserializeObject<T>(content);
                    _logger.LogInformation($"{method} request to {endpoint} succeeded with status {response.StatusCode}");
                    return result;
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, $"Failed to deserialize response from {endpoint}. Content: {content}");
                    
                    // For successful responses, if we can't deserialize but response was successful,
                    // try to return a default success indicator for certain types
                    if (typeof(T) == typeof(object))
                    {
                        _logger.LogInformation($"Returning success indicator for {method} {endpoint} despite deserialization failure");
                        return (T)(object)new { Success = true };
                    }
                    
                    return default;
                }
            }

            // Handle specific error status codes
            var errorMessage = await GetDetailedErrorMessage(response, content, endpoint, method, requestPayload);
            
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    _logger.LogWarning($"Unauthorized access to {endpoint}");
                    break;
                case HttpStatusCode.Forbidden:
                    _logger.LogWarning($"Forbidden access to {endpoint}");
                    break;
                case HttpStatusCode.NotFound:
                    _logger.LogWarning($"Resource not found: {endpoint}");
                    break;
                case HttpStatusCode.BadRequest:
                    _logger.LogError($"Bad request to {endpoint}: {errorMessage}");
                    // For Bad Request, throw exception with parsed error message
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
                        var message = errorResponse?.message?.ToString() ?? "Bad Request";
                        throw new HttpRequestException($"400: {message}");
                    }
                    catch (JsonException)
                    {
                        throw new HttpRequestException($"400: Bad Request - {content}");
                    }
                case HttpStatusCode.InternalServerError:
                    _logger.LogError($"Server error (500) for {endpoint}: {errorMessage}");
                    // For 500 errors, throw an exception so retry mechanism can work
                    throw new HttpRequestException($"Server error (500) for {endpoint}: {errorMessage}");
                case HttpStatusCode.ServiceUnavailable:
                    _logger.LogError($"Service unavailable (503) for {endpoint}: {errorMessage}");
                    throw new HttpRequestException($"Service unavailable (503) for {endpoint}: {errorMessage}");
                default:
                    _logger.LogError($"{method} request to {endpoint} failed with status {response.StatusCode}: {errorMessage}");
                    throw new HttpRequestException($"{(int)response.StatusCode}: {response.ReasonPhrase} - {content}");
            }

            return default;
        }

        private async Task<string> GetDetailedErrorMessage(HttpResponseMessage response, string content, string endpoint, string method, string? requestPayload)
        {
            var errorDetails = new StringBuilder();
            errorDetails.AppendLine($"API Error Details:");
            errorDetails.AppendLine($"Endpoint: {method} {endpoint}");
            errorDetails.AppendLine($"Status Code: {response.StatusCode} ({(int)response.StatusCode})");
            errorDetails.AppendLine($"Reason Phrase: {response.ReasonPhrase}");
            
            if (!string.IsNullOrEmpty(requestPayload))
            {
                errorDetails.AppendLine($"Request Payload: {requestPayload}");
            }
            
            if (!string.IsNullOrEmpty(content))
            {
                errorDetails.AppendLine($"Response Content: {content}");
            }

            // Add response headers for debugging
            if (response.Headers.Any())
            {
                errorDetails.AppendLine("Response Headers:");
                foreach (var header in response.Headers)
                {
                    errorDetails.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            var errorMessage = errorDetails.ToString();
            _logger.LogError(errorMessage);
            
            return errorMessage;
        }
    }
}
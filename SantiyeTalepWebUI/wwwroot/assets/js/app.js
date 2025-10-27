!function() {
    'use strict';

    // Prevent multiple initializations
    if (window.appInitialized) {
        return;
    }
    window.appInitialized = true;

    // Global variables
    var navbarMenuHTML = "";
    var maxMenuItems = 7;
    var defaultLanguage = "en";
    var currentLanguage = localStorage.getItem("language");

    // Utility function to safely add event listeners
    function safeAddEventListener(element, event, handler, options) {
        if (element && typeof element.addEventListener === 'function') {
            try {
                element.addEventListener(event, handler, options || false);
            } catch (error) {
                console.error("Error adding event listener:", error);
            }
        }
    }

    // Utility function to safely query elements
    function safeQuerySelector(selector) {
        try {
            return document.querySelector(selector);
        } catch (error) {
            console.error("Error querying selector:", selector, error);
            return null;
        }
    }

    function safeQuerySelectorAll(selector) {
        try {
            return document.querySelectorAll(selector);
        } catch (error) {
            console.error("Error querying selector:", selector, error);
            return [];
        }
    }

    // Initialize navbar menu content
    function initializeNavbarMenu() {
        var navbarMenu = safeQuerySelector(".navbar-menu");
        if (navbarMenu) {
            navbarMenuHTML = navbarMenu.innerHTML;
        }
    }

    // Language functionality
    function initializeLanguage() {
        setLanguage(currentLanguage || defaultLanguage);
        
        var languageElements = document.getElementsByClassName("language");
        if (languageElements && languageElements.length > 0) {
            Array.from(languageElements).forEach(function(element) {
                safeAddEventListener(element, "click", function(e) {
                    var lang = element.getAttribute("data-lang");
                    if (lang) {
                        setLanguage(lang);
                    }
                });
            });
        }
    }

    function setLanguage(language) {
        var headerLangImg = document.getElementById("header-lang-img");
        if (headerLangImg) {
            var flagPath = "assets/images/flags/";
            switch(language) {
                case "en":
                    headerLangImg.src = flagPath + "us.svg";
                    break;
                case "sp":
                    headerLangImg.src = flagPath + "spain.svg";
                    break;
                case "gr":
                    headerLangImg.src = flagPath + "germany.svg";
                    break;
                case "it":
                    headerLangImg.src = flagPath + "italy.svg";
                    break;
                case "ru":
                    headerLangImg.src = flagPath + "russia.svg";
                    break;
                case "ch":
                    headerLangImg.src = flagPath + "china.svg";
                    break;
                case "fr":
                    headerLangImg.src = flagPath + "french.svg";
                    break;
                case "ar":
                    headerLangImg.src = flagPath + "ae.svg";
                    break;
                default:
                    headerLangImg.src = flagPath + "us.svg";
            }
        }
        
        try {
            localStorage.setItem("language", language);
            currentLanguage = language;
        } catch (error) {
            console.error("Error setting language in localStorage:", error);
        }
        
        // Load language data
        loadLanguageData(language);
    }

    function loadLanguageData(language) {
        var xhr = new XMLHttpRequest();
        xhr.open("GET", "assets/lang/" + language + ".json");
        xhr.onreadystatechange = function() {
            if (this.readyState === 4) {
                if (this.status === 200) {
                    try {
                        var langData = JSON.parse(this.responseText);
                        Object.keys(langData).forEach(function(key) {
                            var elements = safeQuerySelectorAll("[data-key='" + key + "']");
                            Array.from(elements).forEach(function(element) {
                                if (element && element.textContent !== undefined) {
                                    element.textContent = langData[key];
                                }
                            });
                        });
                    } catch (error) {
                        console.error("Error parsing language data:", error);
                    }
                } else if (this.status !== 404) {
                    console.warn("Language file request failed:", language, "Status:", this.status);
                }
            }
        };
        
        try {
            xhr.send();
        } catch (error) {
            console.error("Error sending language request:", error);
        }
    }

    // Sidebar collapse functionality
    function initializeCollapse() {
        var collapseElements = safeQuerySelectorAll(".navbar-nav .collapse");
        if (!collapseElements || collapseElements.length === 0 || typeof bootstrap === 'undefined' || !bootstrap.Collapse) {
            return;
        }

        Array.from(collapseElements).forEach(function(collapseElement) {
            try {
                var collapseInstance = new bootstrap.Collapse(collapseElement, { toggle: false });

                safeAddEventListener(collapseElement, "show.bs.collapse", function(e) {
                    e.stopPropagation();
                    
                    var parentCollapse = collapseElement.parentElement ? collapseElement.parentElement.closest(".collapse") : null;
                    if (parentCollapse) {
                        var siblingCollapses = parentCollapse.querySelectorAll(".collapse");
                        Array.from(siblingCollapses).forEach(function(sibling) {
                            var siblingInstance = bootstrap.Collapse.getInstance(sibling);
                            if (siblingInstance && siblingInstance !== collapseInstance) {
                                siblingInstance.hide();
                            }
                        });
                    }
                });

                safeAddEventListener(collapseElement, "hide.bs.collapse", function(e) {
                    e.stopPropagation();
                    var childCollapses = collapseElement.querySelectorAll(".collapse");
                    Array.from(childCollapses).forEach(function(childCollapse) {
                        var childInstance = bootstrap.Collapse.getInstance(childCollapse);
                        if (childInstance) {
                            childInstance.hide();
                        }
                    });
                });
            } catch (error) {
                console.error("Error initializing collapse for element:", error);
            }
        });
    }

    // Back to top functionality
    function initializeBackToTop() {
        var backToTopButton = document.getElementById("back-to-top");
        if (!backToTopButton) return;

        // Remove any existing event listeners by cloning the button
        var newButton = backToTopButton.cloneNode(true);
        if (backToTopButton.parentNode) {
            backToTopButton.parentNode.replaceChild(newButton, backToTopButton);
            backToTopButton = newButton;
        }

        safeAddEventListener(window, 'scroll', function() {
            try {
                if (document.body.scrollTop > 100 || document.documentElement.scrollTop > 100) {
                    backToTopButton.style.display = "block";
                } else {
                    backToTopButton.style.display = "none";
                }
            } catch (error) {
                console.error("Error in scroll function:", error);
            }
        });

        safeAddEventListener(backToTopButton, 'click', function(e) {
            e.preventDefault();
            try {
                if (window.scrollTo) {
                    window.scrollTo({
                        top: 0,
                        behavior: 'smooth'
                    });
                } else {
                    // Fallback for older browsers
                    document.body.scrollTop = 0;
                    document.documentElement.scrollTop = 0;
                }
            } catch (error) {
                // Fallback for any errors
                document.body.scrollTop = 0;
                document.documentElement.scrollTop = 0;
            }
        });
    }

    // Hamburger menu functionality - IMPROVED
    function initializeHamburgerMenu() {
        var hamburgerIcon = document.getElementById("topnav-hamburger-icon");
        if (!hamburgerIcon) {
            console.warn("Hamburger menu button not found!");
            return;
        }

        console.log("Hamburger menu button found, initializing...");

        safeAddEventListener(hamburgerIcon, "click", function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            console.log("Hamburger menu clicked!");
            
            try {
                toggleSidebar();
            } catch (error) {
                console.error("Error in hamburger menu toggle:", error);
            }
        });

        // Also handle vertical overlay click
        var verticalOverlay = safeQuerySelector(".vertical-overlay");
        if (verticalOverlay) {
            safeAddEventListener(verticalOverlay, "click", function() {
                try {
                    closeSidebar();
                } catch (error) {
                    console.error("Error closing sidebar via overlay:", error);
                }
            });
        }
    }

    function toggleSidebar() {
        var body = document.body;
        var hamburgerIcon = safeQuerySelector(".hamburger-icon");
        var appMenu = safeQuerySelector(".app-menu");
        var verticalOverlay = safeQuerySelector(".vertical-overlay");
        
        console.log("Toggling sidebar...");

        // Toggle hamburger icon animation
        if (hamburgerIcon) {
            hamburgerIcon.classList.toggle("open");
            console.log("Hamburger icon toggled, open class:", hamburgerIcon.classList.contains("open"));
        }

        // Get current screen width
        var clientWidth = document.documentElement.clientWidth;
        var layout = document.documentElement.getAttribute("data-layout") || "vertical";
        
        console.log("Client width:", clientWidth, "Layout:", layout);

        // Handle mobile screens (767px and below)
        if (clientWidth <= 767) {
            if (body.classList.contains("vertical-sidebar-enable")) {
                // Close sidebar on mobile
                body.classList.remove("vertical-sidebar-enable");
                if (verticalOverlay) {
                    verticalOverlay.style.display = "none";
                }
                console.log("Mobile sidebar closed");
            } else {
                // Open sidebar on mobile
                body.classList.add("vertical-sidebar-enable");
                if (verticalOverlay) {
                    verticalOverlay.style.display = "block";
                }
                console.log("Mobile sidebar opened");
            }
            return;
        }

        // Handle tablet screens (768px to 1024px)
        if (clientWidth > 767 && clientWidth <= 1024) {
            var currentSize = document.documentElement.getAttribute("data-sidebar-size");
            if (currentSize === "sm") {
                document.documentElement.setAttribute("data-sidebar-size", "lg");
                console.log("Tablet: Expanded sidebar to lg");
            } else {
                document.documentElement.setAttribute("data-sidebar-size", "sm");
                console.log("Tablet: Collapsed sidebar to sm");
            }
            return;
        }

        // Handle desktop screens (1025px and above)
        if (clientWidth > 1024) {
            var currentSize = document.documentElement.getAttribute("data-sidebar-size");
            if (currentSize === "lg") {
                document.documentElement.setAttribute("data-sidebar-size", "sm");
                console.log("Desktop: Collapsed sidebar to sm");
            } else {
                document.documentElement.setAttribute("data-sidebar-size", "lg");
                console.log("Desktop: Expanded sidebar to lg");
            }
            return;
        }
    }

    function closeSidebar() {
        var body = document.body;
        var hamburgerIcon = safeQuerySelector(".hamburger-icon");
        var verticalOverlay = safeQuerySelector(".vertical-overlay");
        var clientWidth = document.documentElement.clientWidth;

        console.log("Closing sidebar...");

        // Close hamburger icon animation
        if (hamburgerIcon) {
            hamburgerIcon.classList.remove("open");
        }

        // Close mobile sidebar
        if (clientWidth <= 767) {
            body.classList.remove("vertical-sidebar-enable");
            if (verticalOverlay) {
                verticalOverlay.style.display = "none";
            }
        }
    }

    // Handle window resize for responsive sidebar behavior
    function handleResize() {
        var clientWidth = document.documentElement.clientWidth;
        var body = document.body;
        var verticalOverlay = safeQuerySelector(".vertical-overlay");
        
        console.log("Window resized, width:", clientWidth);

        // Reset mobile sidebar state on resize
        if (clientWidth > 767) {
            body.classList.remove("vertical-sidebar-enable");
            if (verticalOverlay) {
                verticalOverlay.style.display = "none";
            }
        }

        // Update feather icons if available
        if (typeof feather !== 'undefined') {
            feather.replace();
        }
    }

    // Fullscreen functionality
    function initializeFullscreen() {
        var fullscreenButton = safeQuerySelector('[data-toggle="fullscreen"]');
        if (!fullscreenButton) return;

        safeAddEventListener(fullscreenButton, 'click', function(e) {
            e.preventDefault();
            
            try {
                if (!document.fullscreenElement && !document.mozFullScreenElement && !document.webkitFullscreenElement) {
                    // Enter fullscreen
                    var elem = document.documentElement;
                    if (elem.requestFullscreen) {
                        elem.requestFullscreen();
                    } else if (elem.mozRequestFullScreen) {
                        elem.mozRequestFullScreen();
                    } else if (elem.webkitRequestFullscreen) {
                        elem.webkitRequestFullscreen();
                    } else if (elem.msRequestFullscreen) {
                        elem.msRequestFullscreen();
                    }
                    document.body.classList.add("fullscreen-enable");
                } else {
                    // Exit fullscreen
                    if (document.exitFullscreen) {
                        document.exitFullscreen();
                    } else if (document.mozCancelFullScreen) {
                        document.mozCancelFullScreen();
                    } else if (document.webkitExitFullscreen) {
                        document.webkitExitFullscreen();
                    } else if (document.msExitFullscreen) {
                        document.msExitFullscreen();
                    }
                    document.body.classList.remove("fullscreen-enable");
                }
            } catch (error) {
                console.error("Error toggling fullscreen:", error);
            }
        });
    }

    // Light/Dark mode functionality
    function initializeLightDarkMode() {
        var lightDarkButton = safeQuerySelector('.light-dark-mode');
        if (!lightDarkButton) return;

        safeAddEventListener(lightDarkButton, 'click', function(e) {
            e.preventDefault();
            try {
                var html = document.documentElement;
                var currentTheme = html.getAttribute('data-bs-theme') || 'light';
                
                if (currentTheme === 'dark') {
                    html.setAttribute('data-bs-theme', 'light');
                    html.setAttribute('data-topbar', 'light');
                    html.setAttribute('data-sidebar', 'light');
                    try {
                        sessionStorage.setItem('data-bs-theme', 'light');
                    } catch (storageError) {
                        console.warn("Could not save theme to sessionStorage:", storageError);
                    }
                } else {
                    html.setAttribute('data-bs-theme', 'dark');
                    html.setAttribute('data-topbar', 'dark'); 
                    html.setAttribute('data-sidebar', 'dark');
                    try {
                        sessionStorage.setItem('data-bs-theme', 'dark');
                    } catch (storageError) {
                        console.warn("Could not save theme to sessionStorage:", storageError);
                    }
                }

                // Trigger resize event to update charts
                window.dispatchEvent(new Event('resize'));
            } catch (error) {
                console.error("Error toggling light/dark mode:", error);
            }
        });
    }

    // Set active menu item
    function setActiveMenuItem() {
        try {
            var currentPath = location.pathname;
            var navLinks = safeQuerySelectorAll('.navbar-nav .nav-link');
            
            Array.from(navLinks).forEach(function(link) {
                var href = link.getAttribute('href');
                if (href && currentPath.indexOf(href) !== -1 && href !== '#' && href !== '/') {
                    link.classList.add('active');
                    
                    // If it's in a submenu, expand the parent
                    var parentCollapse = link.closest('.menu-dropdown');
                    if (parentCollapse) {
                        parentCollapse.classList.add('show');
                        var parentLink = safeQuerySelector('[href="#' + parentCollapse.id + '"]');
                        if (parentLink) {
                            parentLink.setAttribute('aria-expanded', 'true');
                            parentLink.classList.remove('collapsed');
                        }
                    }
                }
            });
        } catch (error) {
            console.error("Error setting active menu item:", error);
        }
    }

    // Topbar shadow on scroll
    function initializeTopbarShadow() {
        var topbar = document.getElementById("page-topbar");
        if (!topbar) return;

        safeAddEventListener(window, 'scroll', function() {
            try {
                if (document.body.scrollTop > 50 || document.documentElement.scrollTop > 50) {
                    topbar.classList.add("topbar-shadow");
                } else {
                    topbar.classList.remove("topbar-shadow");
                }
            } catch (error) {
                console.error("Error updating topbar shadow:", error);
            }
        });
    }

    // Preloader functionality
    function initializePreloader() {
        var preloader = document.getElementById("preloader");
        if (!preloader) return;

        safeAddEventListener(window, 'load', function() {
            try {
                setTimeout(function() {
                    if (preloader) {
                        preloader.style.opacity = "0";
                        preloader.style.visibility = "hidden";
                    }
                }, 500);
            } catch (error) {
                console.error("Error hiding preloader:", error);
            }
        });
    }

    // Main initialization function
    function initialize() {
        try {
            console.log("Initializing app...");
            
            // Initialize core functionality
            initializeNavbarMenu();
            initializeLanguage();
            initializeCollapse();
            initializeHamburgerMenu(); // This should initialize the hamburger menu
            initializeBackToTop();
            initializeFullscreen();
            initializeLightDarkMode();
            initializeTopbarShadow();
            initializePreloader();

            // Initialize external libraries
            if (typeof feather !== 'undefined') {
                feather.replace();
            }

            if (typeof Waves !== 'undefined') {
                Waves.init();
            }

            // Set up resize handler
            safeAddEventListener(window, 'resize', handleResize);

            // Set active menu item
            setActiveMenuItem();

            console.log("App initialized successfully");

        } catch (error) {
            console.error("Error during app initialization:", error);
        }
    }

    // Global topFunction for back to top button
    window.topFunction = function() {
        try {
            if (window.scrollTo) {
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            } else {
                document.body.scrollTop = 0;
                document.documentElement.scrollTop = 0;
            }
        } catch (error) {
            document.body.scrollTop = 0;
            document.documentElement.scrollTop = 0;
        }
    };

    // Initialize the application with proper DOM ready handling
    function domReady(callback) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', callback);
        } else {
            // DOM is already ready
            setTimeout(callback, 1);
        }
    }

    // Start initialization
    domReady(initialize);

}();
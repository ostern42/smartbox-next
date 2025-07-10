// Layout Manager for SmartBox
class LayoutManager {
    constructor() {
        this.currentLayout = 'standard';
        this.currentTheme = 'default';
        this.layouts = {
            standard: {
                name: 'Standard',
                description: 'Optimized for 1920x1080',
                css: 'styles/layout-standard.css',
                mwlSidebarWidth: 350,
                showThumbnails: true,
                showPatientInfo: true
            },
            compact: {
                name: 'Compact',
                description: 'For smaller screens (1280x1024)',
                css: 'styles/layout-compact.css',
                mwlSidebarWidth: 280,
                showThumbnails: true,
                showPatientInfo: true
            },
            minimal: {
                name: 'Minimal',
                description: 'Maximum capture area',
                css: 'styles/layout-minimal.css',
                mwlSidebarWidth: 0, // Hidden by default
                showThumbnails: false,
                showPatientInfo: false
            },
            mwlFocus: {
                name: 'MWL Focus',
                description: 'Larger worklist area',
                css: 'styles/layout-mwl-focus.css',
                mwlSidebarWidth: 500,
                showThumbnails: true,
                showPatientInfo: true
            }
        };
        
        this.themes = {
            default: {
                name: 'Default Light',
                css: 'styles/theme-default.css'
            },
            dark: {
                name: 'Dark Mode',
                css: 'styles/theme-default.css',
                bodyClass: 'theme-dark'
            },
            night: {
                name: 'Night Mode',
                css: 'styles/theme-night.css',
                bodyClass: 'theme-night'
            },
            highcontrast: {
                name: 'High Contrast',
                css: 'styles/theme-default.css',
                bodyClass: 'theme-highcontrast'
            }
        };
        
        this.init();
    }
    
    init() {
        // Load saved preferences
        const savedLayout = localStorage.getItem('smartbox-layout') || 'standard';
        const savedTheme = localStorage.getItem('smartbox-theme') || 'default';
        
        // Detect screen size and suggest layout
        this.detectOptimalLayout();
        
        // Apply saved preferences
        this.setLayout(savedLayout);
        this.setTheme(savedTheme);
        
        // Listen for window resize
        window.addEventListener('resize', () => this.handleResize());
    }
    
    detectOptimalLayout() {
        const width = window.innerWidth;
        const height = window.innerHeight;
        
        if (width <= 1366 && height <= 1024) {
            this.suggestedLayout = 'compact';
        } else if (width >= 1920 && height >= 1080) {
            this.suggestedLayout = 'standard';
        } else {
            this.suggestedLayout = 'standard';
        }
        
        // Show suggestion if different from current
        if (this.suggestedLayout !== this.currentLayout && !localStorage.getItem('smartbox-layout')) {
            this.showLayoutSuggestion();
        }
    }
    
    showLayoutSuggestion() {
        const suggestion = document.createElement('div');
        suggestion.className = 'layout-suggestion';
        suggestion.innerHTML = `
            <p>We detected your screen resolution. Would you like to switch to ${this.layouts[this.suggestedLayout].name} layout?</p>
            <button onclick="layoutManager.setLayout('${this.suggestedLayout}')">Yes, switch</button>
            <button onclick="this.parentElement.remove()">No, keep current</button>
        `;
        document.body.appendChild(suggestion);
        
        // Auto-hide after 10 seconds
        setTimeout(() => suggestion.remove(), 10000);
    }
    
    setLayout(layoutName) {
        if (!this.layouts[layoutName]) return;
        
        // Remove previous layout CSS
        const oldLink = document.getElementById('layout-css');
        if (oldLink) oldLink.remove();
        
        // Add new layout CSS
        const link = document.createElement('link');
        link.id = 'layout-css';
        link.rel = 'stylesheet';
        link.href = this.layouts[layoutName].css;
        document.head.appendChild(link);
        
        // Apply layout-specific changes
        this.currentLayout = layoutName;
        this.applyLayoutSettings(this.layouts[layoutName]);
        
        // Save preference
        localStorage.setItem('smartbox-layout', layoutName);
        
        // Notify other components
        window.dispatchEvent(new CustomEvent('layoutChanged', { 
            detail: { layout: layoutName, settings: this.layouts[layoutName] }
        }));
    }
    
    setTheme(themeName) {
        if (!this.themes[themeName]) return;
        
        // Remove previous theme CSS
        const oldLink = document.getElementById('theme-css');
        if (oldLink) oldLink.remove();
        
        // Add new theme CSS
        const link = document.createElement('link');
        link.id = 'theme-css';
        link.rel = 'stylesheet';
        link.href = this.themes[themeName].css;
        document.head.appendChild(link);
        
        // Remove all theme body classes
        Object.values(this.themes).forEach(theme => {
            if (theme.bodyClass) {
                document.body.classList.remove(theme.bodyClass);
            }
        });
        
        // Add new theme body class
        if (this.themes[themeName].bodyClass) {
            document.body.classList.add(this.themes[themeName].bodyClass);
        }
        
        this.currentTheme = themeName;
        localStorage.setItem('smartbox-theme', themeName);
        
        // Notify other components
        window.dispatchEvent(new CustomEvent('themeChanged', { 
            detail: { theme: themeName }
        }));
    }
    
    applyLayoutSettings(settings) {
        // Apply MWL sidebar width
        const mwlSidebar = document.querySelector('.mwl-sidebar');
        if (mwlSidebar) {
            if (settings.mwlSidebarWidth === 0) {
                mwlSidebar.classList.add('collapsed');
            } else {
                mwlSidebar.classList.remove('collapsed');
                mwlSidebar.style.width = settings.mwlSidebarWidth + 'px';
            }
        }
        
        // Show/hide thumbnails
        const thumbnails = document.querySelector('.thumbnails-container');
        if (thumbnails) {
            thumbnails.style.display = settings.showThumbnails ? 'flex' : 'none';
        }
        
        // Show/hide patient info
        const patientInfo = document.querySelector('.patient-info');
        if (patientInfo) {
            patientInfo.style.display = settings.showPatientInfo ? 'flex' : 'none';
        }
    }
    
    handleResize() {
        // Debounce resize events
        clearTimeout(this.resizeTimeout);
        this.resizeTimeout = setTimeout(() => {
            this.detectOptimalLayout();
        }, 250);
    }
    
    // Quick access methods
    toggleTheme() {
        const themes = Object.keys(this.themes);
        const currentIndex = themes.indexOf(this.currentTheme);
        const nextIndex = (currentIndex + 1) % themes.length;
        this.setTheme(themes[nextIndex]);
    }
    
    toggleMwlSidebar() {
        const sidebar = document.querySelector('.mwl-sidebar');
        if (sidebar) {
            sidebar.classList.toggle('collapsed');
        }
    }
    
    enterFullscreen() {
        if (document.documentElement.requestFullscreen) {
            document.documentElement.requestFullscreen();
        }
    }
    
    exitFullscreen() {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        }
    }
}

// Initialize layout manager when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.layoutManager = new LayoutManager();
});
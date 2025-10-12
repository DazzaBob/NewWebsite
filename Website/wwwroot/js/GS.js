// ============================================================================
// Name: Global Script (JS) Utilities
// File: GS.js
// Description: Contains global JavaScript utilities for the site, such as overlay spinner control, modal helpers, and other shared functions.
// Author: Darrell Roberts
// Created: 2025-09-30
// ============================================================================
let _globalSpinner = null;

function toggleGlobalSpinner(show = false) { // Create the spinner automatically
    if (!_globalSpinner) {
        _globalSpinner = document.createElement('div');
        _globalSpinner.id = 'globalOverlaySpinner';
        Object.assign(_globalSpinner.style, {
            position: 'fixed',
            top: '0',
            left: '0',
            width: '100vw',
            height: '100vh',
            background: 'rgba(0,0,0,0.3)',
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            zIndex: '9999',
            visibility: 'hidden'
        });
        _globalSpinner.innerHTML = `<div class="spinner spinner--lg"></div>`;
        document.body.appendChild(_globalSpinner);
    }

    _globalSpinner.style.visibility = show ? 'visible' : 'hidden';
}
function adjustMainPadding() {
    const header = document.querySelector('.site-header');
    const footer = document.querySelector('.site-footer');
    const main = document.querySelector('.site-main');

    if (!main) return;

    const headerHeight = header ? header.offsetHeight : 0;
    const footerHeight = footer ? footer.offsetHeight : 0;

    // Apply dynamic padding
    main.style.paddingTop = headerHeight + 'px';
    main.style.paddingBottom = footerHeight + 'px';
}

// Run on page load
window.addEventListener('load', adjustMainPadding);

// Optional: Run on resize to handle responsive breakpoints
window.addEventListener('resize', adjustMainPadding);
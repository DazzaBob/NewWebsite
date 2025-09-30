// ============================================================================
// Name: Global Script (JS) Utilities
// File: GS.js
// Description: Contains global JavaScript utilities for the site, such as overlay spinner control, modal helpers, and other shared functions.
// Author: Darrell Roberts
// Created: 2025-09-30
// ============================================================================
let _globalSpinner = null;
function toggleGlobalSpinner(show = false) {
    if (!_globalSpinner) {
        _globalSpinner = document.getElementById('globalOverlaySpinner');
        if (!_globalSpinner) {
            _globalSpinner = document.createElement('div');
            _globalSpinner.id = 'globalOverlaySpinner';
            Object.assign(_globalSpinner.style, {
                position: 'fixed',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
                zIndex: '9999',
                display: 'none'
            });
            _globalSpinner.innerHTML = `<div class="spinner spinner--lg"></div>`;
            document.body.appendChild(_globalSpinner);
        }
    }
    _globalSpinner.style.display = show ? 'block' : 'none';
}
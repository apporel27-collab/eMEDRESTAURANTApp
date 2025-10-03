/* 
    This file contains the added debug helpers for the menu item selection.
    Include this in the Details.cshtml page to help diagnose selection issues.
*/

function setupMenuItemDebugger() {
    // Create debug element
    const debugContainer = document.createElement('div');
    debugContainer.id = 'menuItemDebug';
    debugContainer.style.cssText = 'position:fixed; bottom:10px; right:10px; background:rgba(0,0,0,0.8); color:white; padding:10px; border-radius:5px; max-width:350px; z-index:9999; font-size:12px; max-height:200px; overflow:auto;';
    debugContainer.innerHTML = '<h6 style="margin:0 0 5px 0">Menu Item Selection Debug</h6><div id="menuDebugContent"></div>';
    
    // Add minimize button
    const minimizeBtn = document.createElement('button');
    minimizeBtn.textContent = 'X';
    minimizeBtn.style.cssText = 'position:absolute; top:5px; right:5px; background:none; border:none; color:white; cursor:pointer; font-weight:bold;';
    minimizeBtn.onclick = function() {
        const content = document.getElementById('menuDebugContent');
        if (content.style.display === 'none') {
            content.style.display = 'block';
            this.textContent = 'X';
        } else {
            content.style.display = 'none';
            this.textContent = '+';
        }
    };
    debugContainer.appendChild(minimizeBtn);
    
    document.body.appendChild(debugContainer);
    
    // Setup input monitoring
    const menuItemInput = document.getElementById('menuItemInput');
    if (menuItemInput) {
        // Track input changes
        menuItemInput.addEventListener('input', function() {
            updateMenuDebug(`Input changed: "${this.value}"`);
        });
        
        // Track datalist selection
        menuItemInput.addEventListener('change', function() {
            updateMenuDebug(`Selection changed: "${this.value}"`);
        });
    }
    
    // Track button clicks
    const addButton = document.getElementById('quickAddButton');
    if (addButton) {
        addButton.addEventListener('click', function() {
            updateMenuDebug('Add button clicked');
        });
    }
    
    updateMenuDebug('Menu item debugger initialized');
}

function updateMenuDebug(message) {
    const content = document.getElementById('menuDebugContent');
    if (content) {
        const time = new Date().toLocaleTimeString();
        content.innerHTML = `<div>[${time}] ${message}</div>` + content.innerHTML;
        
        // Limit entries
        const entries = content.querySelectorAll('div');
        if (entries.length > 20) {
            content.removeChild(entries[entries.length - 1]);
        }
    }
}

// Initialize debug helper when document is ready
document.addEventListener('DOMContentLoaded', function() {
    setupMenuItemDebugger();
});
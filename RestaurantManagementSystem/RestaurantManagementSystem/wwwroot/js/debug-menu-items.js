// debug-menu-items.js
// This script helps debug any issues with menu item functionality

(function() {
    // Run when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initDebugger();
    });
    
    // Also run on window load as a fallback
    window.addEventListener('load', function() {
        setTimeout(initDebugger, 800);
    });
    
    function initDebugger() {
        console.log('[DEBUG] Initializing menu item debugger');
        
        // Create a log function that's exposed globally
        window.logMenuItemDebug = function(message, data) {
            console.log(`[MENU-ITEM-DEBUG] ${message}`, data);
        };
        
        // Log initial state
        logInitialState();
        
        // Add event listeners for form submissions
        captureFormSubmissions();
        
        console.log('[DEBUG] Menu item debugger initialized');
    }
    
    function logInitialState() {
        try {
            console.log('[DEBUG] Quick Add Form:', document.getElementById('quickAddForm'));
            console.log('[DEBUG] Quick Add Button:', document.getElementById('quickAddButton'));
            console.log('[DEBUG] Menu Item Input:', document.getElementById('menuItemInput'));
            console.log('[DEBUG] Quantity Input:', document.getElementById('quantity'));
            console.log('[DEBUG] Menu Items Datalist:', document.getElementById('menuItems'));
            
            // Count datalist options
            const options = document.querySelectorAll('#menuItems option');
            console.log('[DEBUG] Datalist options count:', options.length);
            
            // Log the first few options
            const sampleOptions = Array.from(options).slice(0, 5);
            console.log('[DEBUG] Sample datalist options:', sampleOptions);
            
            // Check for any inline event handlers
            const inlineHandlers = document.querySelectorAll('[onclick], [onsubmit]');
            console.log('[DEBUG] Inline event handlers count:', inlineHandlers.length);
            
            // Log our functions
            console.log('[DEBUG] directAddMenuItem function:', typeof window.directAddMenuItem);
            console.log('[DEBUG] guaranteedOrderSave function:', typeof window.guaranteedOrderSave);
            console.log('[DEBUG] updateExistingItemsMap function:', typeof window.updateExistingItemsMap);
            
            // Log global tracking variables
            console.log('[DEBUG] existingItemsMap:', window.existingItemsMap);
            console.log('[DEBUG] newOrderItems:', window.newOrderItems);
        } catch (err) {
            console.error('[DEBUG] Error logging initial state:', err);
        }
    }
    
    function captureFormSubmissions() {
        try {
            // Capture the submitOrderForm
            const orderForm = document.getElementById('submitOrderForm');
            if (orderForm) {
                const originalSubmit = orderForm.submit;
                orderForm.submit = function() {
                    console.log('[DEBUG] submitOrderForm.submit() called');
                    console.log('[DEBUG] Form data:', new FormData(orderForm));
                    return originalSubmit.apply(this, arguments);
                };
                
                orderForm.addEventListener('submit', function(event) {
                    console.log('[DEBUG] submitOrderForm submit event triggered');
                    console.log('[DEBUG] Form data:', new FormData(orderForm));
                });
            }
            
            // Also add click listener to the save button
            const saveButton = document.getElementById('saveOrderDetailsBtn');
            if (saveButton) {
                saveButton.addEventListener('click', function(event) {
                    console.log('[DEBUG] Save button clicked');
                    logFormState();
                });
            }
            
            // Add click listener to the quick add button
            const quickAddButton = document.getElementById('quickAddButton');
            if (quickAddButton) {
                quickAddButton.addEventListener('click', function(event) {
                    console.log('[DEBUG] Quick add button clicked');
                    const menuItemInput = document.getElementById('menuItemInput');
                    const quantityInput = document.getElementById('quantity');
                    console.log('[DEBUG] Menu item input value:', menuItemInput ? menuItemInput.value : 'N/A');
                    console.log('[DEBUG] Quantity input value:', quantityInput ? quantityInput.value : 'N/A');
                });
            }
            
            // Add submit listener to the quick add form
            const quickAddForm = document.getElementById('quickAddForm');
            if (quickAddForm) {
                quickAddForm.addEventListener('submit', function(event) {
                    console.log('[DEBUG] Quick add form submitted');
                    const menuItemInput = document.getElementById('menuItemInput');
                    const quantityInput = document.getElementById('quantity');
                    console.log('[DEBUG] Menu item input value:', menuItemInput ? menuItemInput.value : 'N/A');
                    console.log('[DEBUG] Quantity input value:', quantityInput ? quantityInput.value : 'N/A');
                });
            }
        } catch (err) {
            console.error('[DEBUG] Error capturing form submissions:', err);
        }
    }
    
    function logFormState() {
        try {
            // Log the state of the form before submission
            const orderForm = document.getElementById('submitOrderForm');
            if (orderForm) {
                console.log('[DEBUG] Form action:', orderForm.action);
                console.log('[DEBUG] Form method:', orderForm.method);
                console.log('[DEBUG] Form enctype:', orderForm.enctype);
                
                // Log all form inputs
                const inputs = orderForm.querySelectorAll('input, select, textarea');
                console.log('[DEBUG] Form inputs count:', inputs.length);
                
                // Check if we have NewItems in the form
                const newItemInputs = Array.from(inputs).filter(input => input.name && input.name.includes('NewItems'));
                console.log('[DEBUG] New item inputs count:', newItemInputs.length);
                
                // If using our tracking, show what would be submitted
                if (window.newOrderItems && window.newOrderItems.length > 0) {
                    console.log('[DEBUG] newOrderItems to be submitted:', window.newOrderItems);
                }
            }
        } catch (err) {
            console.error('[DEBUG] Error logging form state:', err);
        }
    }
    
    // Add a global function to test the menu item add functionality
    window.testAddMenuItem = function(name, quantity) {
        try {
            console.log('[DEBUG] Testing add menu item:', name, quantity);
            
            // Set the values in the form
            const menuItemInput = document.getElementById('menuItemInput');
            const quantityInput = document.getElementById('quantity');
            
            if (menuItemInput && quantityInput) {
                menuItemInput.value = name || 'Test Item';
                quantityInput.value = quantity || 1;
                
                // Call our direct add function
                if (typeof window.directAddMenuItem === 'function') {
                    console.log('[DEBUG] Calling directAddMenuItem');
                    return window.directAddMenuItem();
                } else {
                    console.log('[DEBUG] directAddMenuItem not found, trying fallback');
                    
                    // Try the fallback button
                    const fallbackBtn = document.getElementById('fallbackAddButton');
                    if (fallbackBtn) {
                        console.log('[DEBUG] Clicking fallback button');
                        fallbackBtn.click();
                        return true;
                    }
                    
                    // Try the original button
                    const quickAddBtn = document.getElementById('quickAddButton');
                    if (quickAddBtn) {
                        console.log('[DEBUG] Clicking quick add button');
                        quickAddBtn.click();
                        return true;
                    }
                }
            } else {
                console.error('[DEBUG] Could not find menu item input or quantity input');
            }
        } catch (err) {
            console.error('[DEBUG] Error testing add menu item:', err);
        }
        
        return false;
    };
})();
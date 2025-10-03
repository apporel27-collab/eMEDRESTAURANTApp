// complete-order-manager.js
// This script integrates menu item add functionality with order saving

(function() {
    // Run when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initCompleteOrderManager();
    });
    
    // Also run on window load as a fallback
    window.addEventListener('load', function() {
        setTimeout(initCompleteOrderManager, 700);
    });
    
    function initCompleteOrderManager() {
        console.log('[ORDER-MANAGER] Initializing complete order manager');
        
        // Enhance the guaranteed order save function to include new items
        if (typeof window.guaranteedOrderSave === 'function') {
            const originalSave = window.guaranteedOrderSave;
            
            window.guaranteedOrderSave = function() {
                try {
                    // Include new items in the form data
                    includeNewItemsInFormData();
                    
                    // Call the original save function
                    return originalSave.apply(this, arguments);
                } catch (err) {
                    console.error('[ORDER-MANAGER] Error in enhanced guaranteedOrderSave:', err);
                    
                    // Fall back to the original function
                    return originalSave.apply(this, arguments);
                }
            };
            
            console.log('[ORDER-MANAGER] Enhanced guaranteedOrderSave function');
        }
        
        // Function to include new items in the form data
        function includeNewItemsInFormData() {
            try {
                if (!window.newOrderItems || window.newOrderItems.length === 0) {
                    console.log('[ORDER-MANAGER] No new items to include');
                    return;
                }
                
                // Get the order form
                const orderForm = document.getElementById('submitOrderForm');
                if (!orderForm) {
                    console.error('[ORDER-MANAGER] Order form not found');
                    return;
                }
                
                console.log('[ORDER-MANAGER] Including new items in form data:', window.newOrderItems);
                
                // Add the new items to the form
                const existingNewItemInputs = orderForm.querySelectorAll('input[name^="NewItems["]');
                existingNewItemInputs.forEach(input => input.remove());

                window.newOrderItems.forEach((item, index) => {
                    // Create hidden inputs for each item property
                    addHiddenInput(orderForm, `NewItems[${index}].MenuItemId`, item.menuItemId);
                    addHiddenInput(orderForm, `NewItems[${index}].MenuItemName`, item.menuItemName);
                    addHiddenInput(orderForm, `NewItems[${index}].Quantity`, item.quantity);
                    addHiddenInput(orderForm, `NewItems[${index}].UnitPrice`, item.unitPrice);
                    
                    // Special instructions may be updated by the user
                    const row = document.querySelector(`tr[data-temp-id="${item.tempId}"]`);
                    if (row) {
                        const specialInstructionsInput = row.querySelector('input[name="specialInstructions"]');
                        const specialInstructions = specialInstructionsInput ? specialInstructionsInput.value : '';
                        addHiddenInput(orderForm, `NewItems[${index}].SpecialInstructions`, specialInstructions);
                    } else {
                        addHiddenInput(orderForm, `NewItems[${index}].SpecialInstructions`, item.specialInstructions || '');
                    }
                });
                
                console.log('[ORDER-MANAGER] New items included in form data');
                
                // Add helper functions to the global scope
                window.includeNewItemsInFormData = includeNewItemsInFormData;
                
                return true;
            } catch (err) {
                console.error('[ORDER-MANAGER] Error including new items in form data:', err);
                return false;
            }
        }
        
        // Helper function to add a hidden input to a form
        function addHiddenInput(form, name, value) {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = name;
            input.value = value || '';
            form.appendChild(input);
        }
        
        // Function to capture form submissions from other scripts
        function captureFormSubmission() {
            try {
                // Get all forms related to order management
                const forms = document.querySelectorAll('form#submitOrderForm, form.order-form, form#orderForm');
                
                for (let i = 0; i < forms.length; i++) {
                    const form = forms[i];
                    
                    // Skip if we've already captured this form
                    if (form.dataset.capturedByOrderManager === 'true') {
                        continue;
                    }
                    
                    // Mark the form as captured
                    form.dataset.capturedByOrderManager = 'true';
                    
                    // Add our event listener
                    form.addEventListener('submit', function(event) {
                        // Don't prevent submission, but make sure our data is included
                        includeNewItemsInFormData();
                    });
                    
                    console.log('[ORDER-MANAGER] Captured form submission:', form);
                }
            } catch (err) {
                console.error('[ORDER-MANAGER] Error capturing form submission:', err);
            }
        }
        
        // Call functions
        captureFormSubmission();
        
        console.log('[ORDER-MANAGER] Complete order manager initialized');
    }
})();
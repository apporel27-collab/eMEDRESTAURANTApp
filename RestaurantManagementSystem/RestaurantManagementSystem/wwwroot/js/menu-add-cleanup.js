// menu-add-cleanup.js
// This script handles the cleanup of conflicting menu item add functionality

(function() {
    // Run when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        cleanupMenuAddFunctionality();
    });
    
    // Also run on window load as a fallback
    window.addEventListener('load', function() {
        setTimeout(cleanupMenuAddFunctionality, 600);
    });
    
    function cleanupMenuAddFunctionality() {
        console.log('[CLEANUP] Starting menu add functionality cleanup');
        
        // Function to safely remove event listeners and attributes
        function safeRemoveHandler(element, attribute) {
            if (!element) return;
            
            try {
                // Remove any inline handler
                if (element.hasAttribute(attribute)) {
                    console.log(`[CLEANUP] Removing ${attribute} from`, element);
                    element.removeAttribute(attribute);
                }
            } catch (err) {
                console.error('[CLEANUP] Error removing handler:', err);
            }
        }
        
        // Clean up the quick add form
        const quickAddForm = document.getElementById('quickAddForm');
        if (quickAddForm) {
            safeRemoveHandler(quickAddForm, 'onsubmit');
            
            // Also clean up any older implementations
            const oldHandle = quickAddForm._oldSubmitHandler;
            if (oldHandle && typeof oldHandle === 'function') {
                try {
                    quickAddForm.removeEventListener('submit', oldHandle);
                } catch (e) {
                    console.log('[CLEANUP] Could not remove old submit handler');
                }
            }
        }
        
        // Clean up the quick add button
        const quickAddButton = document.getElementById('quickAddButton');
        if (quickAddButton) {
            safeRemoveHandler(quickAddButton, 'onclick');
        }
        
        // Create a backup add button if it doesn't exist
        if (!document.getElementById('fallbackAddButton')) {
            try {
                const quickAddContainer = document.querySelector('.quick-add-container');
                if (quickAddContainer) {
                    const fallbackBtn = document.createElement('button');
                    fallbackBtn.id = 'fallbackAddButton';
                    fallbackBtn.type = 'button';
                    fallbackBtn.className = 'btn btn-primary ms-2';
                    fallbackBtn.innerHTML = '<i class="fas fa-plus"></i> Add Item (Backup)';
                    fallbackBtn.style.display = 'none'; // Hide by default
                    
                    quickAddContainer.appendChild(fallbackBtn);
                    console.log('[CLEANUP] Added fallback button');
                }
            } catch (err) {
                console.error('[CLEANUP] Error creating fallback button:', err);
            }
        }
        
        // Clean up any global addMenuItem functions that might conflict
        if (window.addMenuItem) {
            try {
                // Save a backup of the original function
                window._originalAddMenuItem = window.addMenuItem;
                
                // Replace with our function
                window.addMenuItem = function() {
                    console.log('[CLEANUP] Redirecting to direct add menu item');
                    if (typeof window.directAddMenuItem === 'function') {
                        return window.directAddMenuItem();
                    } else {
                        return window._originalAddMenuItem.apply(this, arguments);
                    }
                };
                
                console.log('[CLEANUP] Replaced global addMenuItem function');
            } catch (err) {
                console.error('[CLEANUP] Error replacing addMenuItem function:', err);
            }
        }
        
        // Add helper functions to expose direct add menu item
        if (typeof window.directAddMenuItem === 'function') {
            // Create a way to add menu items from outside this script
            window.addItemToOrder = function(menuItemId, menuItemName, price, quantity) {
                try {
                    // Set the values in the quick add form
                    const menuItemInput = document.getElementById('menuItemInput');
                    const quantityInput = document.getElementById('quantity');
                    
                    if (menuItemInput && quantityInput) {
                        menuItemInput.value = menuItemName;
                        quantityInput.value = quantity || 1;
                        
                        // Call our direct add function
                        return window.directAddMenuItem();
                    }
                } catch (err) {
                    console.error('[CLEANUP] Error in addItemToOrder:', err);
                }
                
                return false;
            };
            
            // Create a function to remove new items
            window.removeNewItem = function(tempId) {
                try {
                    const row = document.querySelector(`tr[data-temp-id="${tempId}"]`);
                    if (row) {
                        // Find the menu item ID
                        const menuItemId = row.getAttribute('data-menu-item-id');
                        
                        // Remove from our tracking
                        if (window.existingItemsMap && menuItemId) {
                            delete window.existingItemsMap[menuItemId];
                        }
                        
                        if (window.newOrderItems) {
                            window.newOrderItems = window.newOrderItems.filter(item => item.tempId !== tempId);
                        }
                        
                        // Remove the row
                        row.remove();
                        
                        return true;
                    }
                } catch (err) {
                    console.error('[CLEANUP] Error removing new item:', err);
                }
                
                return false;
            };
            
            // Create a function to update new item details
            window.updateNewItemDetails = function(tempId, newQuantity, unitPrice) {
                try {
                    const row = document.querySelector(`tr[data-temp-id="${tempId}"]`);
                    if (row) {
                        // Find the menu item ID
                        const menuItemId = row.getAttribute('data-menu-item-id');
                        
                        // Update quantity display
                        const qtyDisplay = row.querySelector('.qty-display');
                        if (qtyDisplay) {
                            qtyDisplay.textContent = newQuantity;
                        }
                        
                        // Update subtotal
                        const newSubtotal = (newQuantity * unitPrice).toFixed(2);
                        const subtotalDisplay = row.querySelector('.subtotal-display');
                        if (subtotalDisplay) {
                            subtotalDisplay.textContent = '₹' + newSubtotal;
                        }
                        
                        // Update our tracking
                        if (window.existingItemsMap && menuItemId) {
                            window.existingItemsMap[menuItemId].quantity = parseInt(newQuantity);
                        }
                        
                        if (window.newOrderItems) {
                            const item = window.newOrderItems.find(item => item.tempId === tempId);
                            if (item) {
                                item.quantity = parseInt(newQuantity);
                            }
                        }
                        
                        return true;
                    }
                } catch (err) {
                    console.error('[CLEANUP] Error updating new item details:', err);
                }
                
                return false;
            };
        }
        
        console.log('[CLEANUP] Menu add functionality cleanup completed');
    }
})();
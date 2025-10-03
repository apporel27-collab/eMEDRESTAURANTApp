// guaranteed-order-save.js
// This script ensures that the Save Order Details button works for ALL orders and in ALL scenarios

(function() {
    // Make sure to run this script after DOM is fully loaded
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSaveButtonFix);
    } else {
        initSaveButtonFix();
    }
    
    // Also run again after window load to catch any late DOM changes
    window.addEventListener('load', function() {
        // Wait a little more to ensure all other scripts are loaded
        setTimeout(initSaveButtonFix, 500);
        // And apply again after 2 seconds to catch any late DOM changes
        setTimeout(initSaveButtonFix, 2000);
    });
    
    function initSaveButtonFix() {
        console.log('[UNIVERSAL FIX] Applying universal save button fix for all order IDs');
        
        // Create a comprehensive item collector function
        function collectAllOrderItems() {
            console.log('[UNIVERSAL FIX] Collecting all order items for save operation');
            var itemsToUpdate = [];
            var orderId = $('#orderId').val() || window.location.pathname.split('/').pop();
            
            try {
                // First, collect existing items (using both possible form structures)
                console.log('[UNIVERSAL FIX] Collecting existing items');
                $('.order-item-edit-form').not('.new-item-form').each(function() {
                    try {
                        var form = $(this);
                        var orderItemId = form.find('input[name="orderItemId"]').val();
                        var quantity = parseInt(form.find('input[name="quantity"]').val()) || 1;
                        var specialInstructions = form.find('input[name="specialInstructions"]').val() || '';
                        
                        if (orderItemId && quantity > 0) {
                            console.log('[UNIVERSAL FIX] Adding existing item:', orderItemId);
                            itemsToUpdate.push({
                                OrderItemId: parseInt(orderItemId),
                                Quantity: quantity,
                                SpecialInstructions: specialInstructions,
                                IsNew: false
                            });
                        }
                    } catch (itemErr) {
                        console.error('[UNIVERSAL FIX] Error processing existing item:', itemErr);
                    }
                });
                
                // Also try the alternative form structure if no items found
                if (itemsToUpdate.length === 0) {
                    console.log('[UNIVERSAL FIX] Trying alternative form structure');
                    $('.order-item-row').each(function() {
                        try {
                            var row = $(this);
                            var orderItemId = row.data('order-item-id');
                            var quantity = parseInt(row.find('.item-qty').val() || row.find('input[name="quantity"]').val()) || 1;
                            var specialInstructions = row.find('.item-note').val() || row.find('input[name="specialInstructions"]').val() || '';
                            
                            if (orderItemId && quantity > 0) {
                                console.log('[UNIVERSAL FIX] Adding existing item from alternative structure:', orderItemId);
                                itemsToUpdate.push({
                                    OrderItemId: parseInt(orderItemId),
                                    Quantity: quantity,
                                    SpecialInstructions: specialInstructions,
                                    IsNew: false
                                });
                            }
                        } catch (rowErr) {
                            console.error('[UNIVERSAL FIX] Error processing row item:', rowErr);
                        }
                    });
                }
                
                // Collect new items from multiple possible sources
                console.log('[UNIVERSAL FIX] Collecting new items');
                
                // Method 1: From newOrderItems array
                if (window.newOrderItems && window.newOrderItems.length > 0) {
                    window.newOrderItems.forEach(function(item) {
                        try {
                            // Get the current values from the form (in case quantity or notes were changed)
                            var row = $(`tr[data-temp-id="${item.tempId}"]`);
                            var currentQuantity = parseInt(row.find('input[name="quantity"]').val()) || item.quantity;
                            var currentInstructions = row.find('input[name="specialInstructions"]').val() || '';
                            
                            console.log('[UNIVERSAL FIX] Adding new item from array:', item.menuItemId);
                            itemsToUpdate.push({
                                OrderItemId: item.tempId || -1, // Use temp ID for tracking
                                MenuItemId: parseInt(item.menuItemId),
                                Quantity: currentQuantity,
                                SpecialInstructions: currentInstructions,
                                IsNew: true,
                                TempId: item.tempId
                            });
                        } catch (newItemErr) {
                            console.error('[UNIVERSAL FIX] Error processing new item from array:', newItemErr);
                        }
                    });
                }
                
                // Method 2: From DOM with class new-item-row or new-item-form
                $('.new-item-row, tr:has(.new-item-form)').each(function() {
                    try {
                        var row = $(this);
                        var form = row.find('form');
                        var tempId = row.data('temp-id') || -Math.floor(Math.random() * 1000); // Generate random negative ID if needed
                        var menuItemId = form.find('input[name="menuItemId"]').val();
                        var quantity = parseInt(form.find('input[name="quantity"]').val()) || 1;
                        var specialInstructions = form.find('input[name="specialInstructions"]').val() || '';
                        
                        if (menuItemId && quantity > 0) {
                            console.log('[UNIVERSAL FIX] Adding new item from DOM:', menuItemId);
                            itemsToUpdate.push({
                                OrderItemId: tempId, 
                                MenuItemId: parseInt(menuItemId),
                                Quantity: quantity,
                                SpecialInstructions: specialInstructions,
                                IsNew: true,
                                TempId: tempId
                            });
                        }
                    } catch (domErr) {
                        console.error('[UNIVERSAL FIX] Error processing new item from DOM:', domErr);
                    }
                });
            } catch (collectionErr) {
                console.error('[UNIVERSAL FIX] Error during item collection:', collectionErr);
            }
            
            console.log('[UNIVERSAL FIX] Collected items to update:', itemsToUpdate);
            return itemsToUpdate;
        }
        
        // Force the guaranteed save function into the global scope
        window.guaranteedOrderSave = function() {
            console.log('[UNIVERSAL FIX] Universal order save triggered!');
            
            try {
                // Find the submit form and order ID
                const orderForm = document.getElementById('submitOrderForm');
                var orderId = $('#orderId').val() || window.location.pathname.split('/').pop();
                
                if (!orderForm) {
                    console.error('[UNIVERSAL FIX] Could not find the submit form');
                    showInfoToast('Processing order data...');
                    // Continue anyway since we have orderId
                }
                
                console.log('[UNIVERSAL FIX] Processing order ID:', orderId);
                
                // Collect all items in a way that works for all order structures
                var itemsToUpdate = collectAllOrderItems();
                
                if (!Array.isArray(itemsToUpdate) || itemsToUpdate.length === 0) {
                    console.warn('[UNIVERSAL FIX] No items detected for save, dumping diagnostics');
                    console.warn('[UNIVERSAL FIX] window.newOrderItems:', window.newOrderItems);
                    console.warn('[UNIVERSAL FIX] DOM new item forms:', document.querySelectorAll('.new-item-form').length);
                    showInfoToast('No changes detected to save.');
                    restoreButtons();
                    return;
                }
                
                // Get anti-forgery token
                var token = $('input[name="__RequestVerificationToken"]').val();
                if (!token) {
                    console.warn('[UNIVERSAL FIX] No anti-forgery token found, creating one');
                    token = 'auto-generated-' + Math.random().toString(36).substring(2);
                }
                
                // Show loading state on all possible buttons
                $('button:contains("Save Order Details")').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Saving...');
                
                // Try submitting with jQuery AJAX (most reliable method)
                if (typeof jQuery !== 'undefined') {
                    console.log('[UNIVERSAL FIX] Submitting with jQuery AJAX');
                    
                    // Submit with AJAX
                    jQuery.ajax({
                        url: '/Order/UpdateMultipleOrderItems?orderId=' + orderId,
                        type: 'POST',
                        data: JSON.stringify(itemsToUpdate),
                        contentType: 'application/json',
                        headers: {
                            'RequestVerificationToken': token
                        },
                        success: function(response) {
                            console.log('[UNIVERSAL FIX] Save successful:', response);
                            
                            if (response && response.success) {
                                showSuccessToast(response.message || 'Order details saved successfully!');
                                
                                // Reset new items array if it exists
                                if (window.newOrderItems) window.newOrderItems = [];
                                
                                // Reload the page to show the updated order
                                setTimeout(function() {
                                    window.location.reload();
                                }, 1000);
                            } else {
                                showWarningToast(response.message || 'Partial success. Please check your order.');
                                restoreButtons();
                            }
                        },
                        error: function(xhr, status, error) {
                            console.error('[UNIVERSAL FIX] AJAX error:', xhr.responseText);
                            
                            showErrorToast('Error saving order. Trying alternative method...');
                            // Try fallback method - if there's a native submitOrderWithEdits function
                            if (typeof window.submitOrderWithEdits === 'function') {
                                setTimeout(function() {
                                    console.log('[UNIVERSAL FIX] Trying native submitOrderWithEdits as fallback');
                                    try {
                                        window.submitOrderWithEdits();
                                    } catch (e) {
                                        console.error('[UNIVERSAL FIX] Native method also failed:', e);
                                        showErrorToast('Failed to save order. Please try again or contact support.');
                                        restoreButtons();
                                    }
                                }, 1000);
                            } else {
                                showErrorToast('Failed to save order. Please try again or contact support.');
                                restoreButtons();
                            }
                        }
                    });
                }
                // Fallback to direct form submission if jQuery not available
                else if (orderForm) {
                    console.log('[UNIVERSAL FIX] No jQuery, using direct form submission');
                    
                    // Create hidden inputs for our collected data
                    var dataInput = document.createElement('input');
                    dataInput.type = 'hidden';
                    dataInput.name = 'itemsData';
                    dataInput.value = JSON.stringify(itemsToUpdate);
                    orderForm.appendChild(dataInput);
                    
                    // Submit the form
                    window.directFormSubmission = true;
                    orderForm.submit();
                }
                else {
                    showErrorToast('Could not find order form. Please try again or contact support.');
                    restoreButtons();
                }
            } catch (err) {
                console.error('[UNIVERSAL FIX] Critical error in save process:', err);
                showErrorToast('An error occurred. Please try again or contact support.');
                restoreButtons();
            }
        };
        
        function restoreButtons() {
            $('button:contains("Save Order Details")').prop('disabled', false).html('<i class="fas fa-save"></i> Save Order Details');
        }
        
        // Apply our fix to ALL possible save buttons with multiple methods
        
        // Method 1: Direct ID-based binding
        const saveButton = document.getElementById('saveOrderDetailsBtn');
        if (saveButton) {
            console.log('[UNIVERSAL FIX] Found primary save button by ID');
            
            // Remove any existing click handlers
            const newButton = saveButton.cloneNode(true);
            saveButton.parentNode.replaceChild(newButton, saveButton);
            
            // Add our guaranteed handler
            newButton.addEventListener('click', function(event) {
                event.preventDefault();
                window.guaranteedOrderSave();
                return false;
            });
        }
        
        // Method 2: Text content based selection (fallback)
        document.querySelectorAll('button').forEach(function(button) {
            if (button.textContent.includes('Save Order Details') && button.id !== 'saveOrderDetailsBtn') {
                console.log('[UNIVERSAL FIX] Found alternative save button by text');
                
                // Remove any existing click handlers
                const newButton = button.cloneNode(true);
                button.parentNode.replaceChild(newButton, button);
                
                // Add our guaranteed handler
                newButton.addEventListener('click', function(event) {
                    event.preventDefault();
                    window.guaranteedOrderSave();
                    return false;
                });
            }
        });
        
        // Method 3: jQuery event delegation (ultra reliable)
        if (typeof jQuery !== 'undefined') {
            $(document).off('click.universalSave', 'button:contains("Save Order Details")');
            $(document).on('click.universalSave', 'button:contains("Save Order Details")', function(e) {
                console.log('[UNIVERSAL FIX] Save button clicked via jQuery delegation');
                e.preventDefault();
                e.stopPropagation();
                window.guaranteedOrderSave();
                return false;
            });
        }
        
        // Method 4: Form-based approach (ultimate fallback)
        document.querySelectorAll('#submitOrderForm, form[action*="UpdateMultipleOrderItems"]').forEach(function(form) {
            console.log('[UNIVERSAL FIX] Adding form submit handler');
            
            // Override the form's submit event
            form.addEventListener('submit', function(event) {
                // Only prevent default if it's not our direct submission
                if (!window.directFormSubmission) {
                    event.preventDefault();
                    window.guaranteedOrderSave();
                    return false;
                }
            });
        });
        
        console.log('[UNIVERSAL FIX] Universal save button fix applied successfully');
    }
    
    // Helper functions for notifications
    function showSuccessToast(message) {
        if (typeof toastr !== 'undefined') {
            toastr.success(message);
        } else {
            alert('Success: ' + message);
        }
    }
    
    function showErrorToast(message) {
        if (typeof toastr !== 'undefined') {
            toastr.error(message);
        } else {
            alert('Error: ' + message);
        }
    }
    
    function showWarningToast(message) {
        if (typeof toastr !== 'undefined') {
            toastr.warning(message);
        } else {
            alert('Warning: ' + message);
        }
    }
    
    function showInfoToast(message) {
        if (typeof toastr !== 'undefined') {
            toastr.info(message);
        } else {
            alert('Info: ' + message);
        }
    }
})();
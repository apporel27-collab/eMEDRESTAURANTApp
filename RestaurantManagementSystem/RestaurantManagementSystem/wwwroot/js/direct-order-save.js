// direct-order-save.js
// Simplified, reliable implementation of the Save Order Details workflow

(function() {
    function ready(callback) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', callback);
        } else {
            callback();
        }
    }

    ready(initDirectOrderSave);
    window.addEventListener('load', function() {
        setTimeout(initDirectOrderSave, 300);
    });

    function initDirectOrderSave() {
        const saveButtons = getSaveButtons();
        if (saveButtons.length === 0) {
            console.warn('[DIRECT SAVE] Save button not found yet');
            return;
        }

        // Replace each save button with a clean clone so no legacy handlers interfere
        for (let i = 0; i < saveButtons.length; i++) {
            const original = saveButtons[i];
            const clone = original.cloneNode(true);
            original.parentNode.replaceChild(clone, original);
            clone.addEventListener('click', handleSaveClick);
        }

        // Expose our handler globally so any other scripts can trigger it
        window.guaranteedOrderSave = function(event) {
            if (event) {
                event.preventDefault();
            }
            return processOrderSave();
        };

        console.log('[DIRECT SAVE] Direct order save initialized');
    }

    function getSaveButtons() {
        const primary = document.getElementById('saveOrderDetailsBtn');
        const matches = Array.from(document.querySelectorAll('button'))
            .filter(btn => btn.textContent && btn.textContent.indexOf('Save Order Details') !== -1);

        const set = new Set();
        if (primary) {
            set.add(primary);
        }
        matches.forEach(btn => set.add(btn));
        return Array.from(set);
    }

    function handleSaveClick(event) {
        event.preventDefault();
        processOrderSave();
        return false;
    }

    async function processOrderSave() {
        const buttons = getSaveButtons();
        setSavingState(buttons, true);

        try {
            const orderIdInput = document.getElementById('orderId');
            const orderId = orderIdInput ? parseInt(orderIdInput.value, 10) : NaN;
            if (!orderId) {
                console.error('[DIRECT SAVE] Order ID not found');
                notify('error', 'Order ID missing, please refresh the page.');
                setSavingState(buttons, false);
                return;
            }

            const items = collectItems();
            console.log('[DIRECT SAVE] Items prepared for save:', items);

            if (items.length === 0) {
                notify('info', 'No changes detected to save.');
                setSavingState(buttons, false);
                return;
            }

            const tokenField = document.querySelector('input[name="__RequestVerificationToken"]');
            const antiForgeryToken = tokenField ? tokenField.value : null;
            if (!antiForgeryToken) {
                console.warn('[DIRECT SAVE] Anti-forgery token not found');
            }

            const response = await fetch(`/Order/UpdateMultipleOrderItems?orderId=${orderId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgeryToken || ''
                },
                body: JSON.stringify(items)
            });

            if (!response.ok) {
                const text = await response.text();
                console.error('[DIRECT SAVE] Server responded with error:', response.status, text);
                throw new Error(`Save failed (${response.status})`);
            }

            let data = null;
            try {
                data = await response.json();
            } catch (err) {
                console.warn('[DIRECT SAVE] Response was not JSON, attempting to parse as text');
            }

            if (data && data.success) {
                notify('success', data.message || 'Order details saved successfully.');
                window.newOrderItems = [];
                setTimeout(function() {
                    window.location.reload();
                }, 800);
            } else {
                const message = data && data.message ? data.message : 'Could not save order details. Please try again.';
                notify('error', message);
                setSavingState(buttons, false);
            }
        } catch (error) {
            console.error('[DIRECT SAVE] Unexpected error during save:', error);
            notify('error', 'Unexpected error while saving. Please try again.');
            setSavingState(buttons, false);
        }
    }

    function collectItems() {
        const items = [];
        const rows = document.querySelectorAll('#orderItemsTable tbody tr');
        rows.forEach(row => {
            try {
                const form = row.querySelector('form');
                if (!form) {
                    return;
                }

                const formData = new FormData(form);
                const orderItemIdRaw = formData.get('orderItemId');
                const quantityField = form.querySelector('input[name="quantity"]');
                const notesField = form.querySelector('input[name="specialInstructions"]');
                const quantityRaw = formData.get('quantity');
                const notesRaw = formData.get('specialInstructions');
                const menuItemIdRaw = formData.get('menuItemId');

                let quantity = parseInt(quantityRaw, 10);
                if (!quantity || isNaN(quantity)) {
                    if (quantityField) {
                        const fallbackQuantity = parseInt(quantityField.value || quantityField.getAttribute('value'), 10);
                        quantity = isNaN(fallbackQuantity) ? 1 : fallbackQuantity;
                    } else {
                        quantity = 1;
                    }
                }

                let specialInstructions = '';
                if (notesRaw && typeof notesRaw === 'string') {
                    specialInstructions = notesRaw.trim();
                } else if (notesField && typeof notesField.value === 'string') {
                    specialInstructions = notesField.value.trim();
                }
                const orderItemId = orderItemIdRaw ? parseInt(orderItemIdRaw, 10) : null;
                let menuItemId = null;
                if (menuItemIdRaw) {
                    menuItemId = parseInt(menuItemIdRaw, 10);
                }
                if (!menuItemId && row.dataset.menuItemId) {
                    menuItemId = parseInt(row.dataset.menuItemId, 10);
                }

                const rowIsNew = row.classList.contains('new-item-row') || (orderItemId !== null && orderItemId < 0);
                const payload = {
                    OrderItemId: orderItemId || 0,
                    Quantity: quantity,
                    SpecialInstructions: specialInstructions,
                    IsNew: rowIsNew,
                    MenuItemId: rowIsNew ? menuItemId : null,
                    TempId: rowIsNew ? (row.dataset.tempId ? parseInt(row.dataset.tempId, 10) : orderItemId) : null
                };

                if (rowIsNew && !menuItemId) {
                    console.warn('[DIRECT SAVE] Skipping new item without menuItemId', row);
                    return;
                }

                items.push(payload);
            } catch (err) {
                console.error('[DIRECT SAVE] Failed to parse row', err, row);
            }
        });

        return items;
    }

    function setSavingState(buttons, saving) {
        buttons.forEach(btn => {
            if (!btn) return;
            if (saving) {
                btn.disabled = true;
                btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
            } else {
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-save"></i> Save Order Details';
            }
        });
    }

    function notify(type, message) {
        if (typeof toastr !== 'undefined' && toastr[type]) {
            toastr[type](message);
        } else {
            switch (type) {
                case 'success':
                    alert('Success: ' + message);
                    break;
                case 'error':
                    alert('Error: ' + message);
                    break;
                default:
                    alert(message);
            }
        }
    }
})();

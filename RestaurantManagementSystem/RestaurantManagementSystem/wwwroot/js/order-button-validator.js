// order-button-validator.js
// This script checks if the Save Order Details button has been properly configured

(function() {
    // Wait for the page to be fully loaded
    window.addEventListener('load', function() {
        // Wait a bit more to ensure all other scripts have initialized
        setTimeout(validateSaveButtonSetup, 1000);
    });
    
    function validateSaveButtonSetup() {
        console.log('Validating Save Order Details button setup...');
        
        // Check for the button in the DOM
        const saveButtons = document.querySelectorAll('button:contains("Save Order Details"), button.btn-save-order-details');
        console.log(`Found ${saveButtons.length} Save Order Details buttons`);
        
        // Check if our unified handlers are attached
        const hasDirectHandler = typeof window.directSaveOrderDetails === 'function';
        const hasGuaranteedHandler = typeof window.guaranteedOrderSave === 'function';
        console.log(`Direct handler present: ${hasDirectHandler}`);
        console.log(`Guaranteed handler present: ${hasGuaranteedHandler}`);
        
        // Check click handlers (this is approximate since we can't directly inspect attached handlers)
        let buttonWithOnClick = document.querySelector('button[onclick*="submitOrderWithEdits"]');
        console.log(`Button with direct onclick attribute: ${buttonWithOnClick !== null}`);
        
        // Final validation
        if (saveButtons.length > 0 && (hasDirectHandler || hasGuaranteedHandler)) {
            console.log('✅ Save Order Details button appears to be properly configured!');
            console.log('  - Button exists in the DOM');
            console.log(`  - Direct handler: ${hasDirectHandler ? '✓' : '✗'}`);
            console.log(`  - Guaranteed handler: ${hasGuaranteedHandler ? '✓' : '✗'}`);
            
            // Highlight the button briefly to show it's validated
            const buttons = document.querySelectorAll('button:contains("Save Order Details")');
            buttons.forEach(btn => {
                btn.classList.add('button-validated');
                setTimeout(() => btn.classList.remove('button-validated'), 2000);
            });
        } else {
            console.warn('⚠️ Save Order Details button may not be fully configured:');
            if (saveButtons.length === 0) console.warn('  - Button not found in the DOM');
            if (!hasDirectHandler && !hasGuaranteedHandler) console.warn('  - No save handlers are attached');
        }
    }
})();
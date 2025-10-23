// Unified modern order details script
(function(){
  const root = document.getElementById('orderDetailsApp');
  if(!root) return;
  const orderId = root.getAttribute('data-order-id');
  const addForm = document.getElementById('orderItemAddForm');
  const addBtn = document.getElementById('addItemBtn');
  const menuInput = document.getElementById('menuItemInput');
  const qtyInput = document.getElementById('menuItemQty');
  const saveBtn = document.getElementById('saveOrderBtn');
  const tbody = document.getElementById('orderItemsBody');
  const subtotalCell = document.getElementById('orderSubtotalCell');
  const taxCell = document.getElementById('orderTaxCell');
  const totalCell = document.getElementById('orderTotalCell');
  const selectAllFire = document.getElementById('selectAllFire');
  let tempIdCounter = -1;

  function getAntiForgery(){
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
  }

  function parsePrice(text){
    if(!text) return 0; return parseFloat(text.replace(/[^0-9.]/g,''))||0;
  }

  function formatMoney(v){
    return '₹'+v.toFixed(2);
  }

  function recalcTotals(){
    let subtotal = 0;
    tbody.querySelectorAll('tr').forEach(tr=>{
      const subCell = tr.querySelector('.subtotal-cell');
      if(subCell){
        subtotal += parsePrice(subCell.textContent);
      }
    });
    subtotalCell.textContent = formatMoney(subtotal);
    // tax + total: keep existing tax value; recompute total (subtotal + tax for now)
    const tax = parsePrice(taxCell.textContent);
    totalCell.textContent = formatMoney(subtotal + tax);
  }

  function buildExistingRowPayload(tr){
    return {
      OrderItemId: parseInt(tr.getAttribute('data-order-item-id'),10),
      Quantity: parseInt(tr.querySelector('.item-qty').value,10) || 1,
      SpecialInstructions: tr.querySelector('.item-note').value.trim(),
      IsNew: false,
      MenuItemId: null,
      TempId: null
    };
  }

  function buildNewRowPayload(tr){
    return {
      OrderItemId: 0,
      Quantity: parseInt(tr.querySelector('.item-qty').value,10) || 1,
      SpecialInstructions: tr.querySelector('.item-note').value.trim(),
      IsNew: true,
      MenuItemId: parseInt(tr.getAttribute('data-menu-item-id'),10),
      TempId: parseInt(tr.getAttribute('data-temp-id'),10)
    };
  }

  function collectPayload(){
    const rows = Array.from(tbody.querySelectorAll('tr'));
    return rows.map(r => r.classList.contains('existing-item-row') ? buildExistingRowPayload(r) : buildNewRowPayload(r));
  }

  function updateRowSubtotal(tr){
    const qtyEl = tr.querySelector('.item-qty');
    const qty = parseInt(qtyEl.value,10)||1;
    const unitText = tr.querySelector('td:nth-child(5)')?.textContent || tr.querySelector('.unit-price')?.textContent;
    const unit = parsePrice(unitText);
    const subCell = tr.querySelector('.subtotal-cell');
    if(subCell){
      subCell.textContent = formatMoney(qty*unit);
    }
  }

  function attachRowEvents(tr){
    const qtyInput = tr.querySelector('.item-qty');
    if(qtyInput){
      qtyInput.addEventListener('change',()=>{updateRowSubtotal(tr); recalcTotals();});
    }
    const removeBtn = tr.querySelector('.remove-existing, .remove-new');
    if(removeBtn){
      removeBtn.addEventListener('click',()=>{tr.remove(); recalcTotals();});
    }
    const editBtn = tr.querySelector('.edit-row');
    if(editBtn){
      const noteInput = tr.querySelector('.item-note');
      const qty = tr.querySelector('.item-qty');
      const saveBtn = tr.querySelector('.save-row');
      const cancelBtn = tr.querySelector('.cancel-row');
      let original = { qty: qty?.value, note: noteInput?.value };
      editBtn.addEventListener('click', ()=>{
        original = { qty: qty.value, note: noteInput.value };
        qty.disabled = false; noteInput.disabled = false;
        editBtn.classList.add('d-none');
        saveBtn.classList.remove('d-none');
        cancelBtn.classList.remove('d-none');
        qty.focus();
      });
      saveBtn?.addEventListener('click', ()=>{
        qty.disabled = true; noteInput.disabled = true;
        saveBtn.classList.add('d-none');
        cancelBtn.classList.add('d-none');
        editBtn.classList.remove('d-none');
        // Mark row dirty implicitly by recalculating subtotal to ensure payload built
        updateRowSubtotal(tr); recalcTotals();
      });
      cancelBtn?.addEventListener('click', ()=>{
        qty.value = original.qty; noteInput.value = original.note;
        qty.disabled = true; noteInput.disabled = true;
        saveBtn.classList.add('d-none');
        cancelBtn.classList.add('d-none');
        editBtn.classList.remove('d-none');
        updateRowSubtotal(tr); recalcTotals();
      });
    }
  }

  function resolveMenuItemByName(name){
    const options = document.querySelectorAll('#menuItems option');
    const target = name.trim().toLowerCase();
    for(const opt of options){
      const optName = (opt.value||'').trim().toLowerCase();
      const optPlu = (opt.getAttribute('data-plu')||'').trim().toLowerCase();
      if(optName === target || optPlu === target){
        return {
          id: parseInt(opt.getAttribute('data-id'),10),
          price: parseFloat(opt.getAttribute('data-price')),
          displayName: opt.getAttribute('data-plu') && optPlu === target ? opt.textContent.split(' - ').slice(1).join(' - ').trim() : opt.value
        };
      }
    }
    return null;
  }

  function addNewItem(name, qty){
    const resolved = resolveMenuItemByName(name);
    if(!resolved){
      toast('Menu item not found','error');
      return;
    }
    if(qty < 1) qty = 1;
    const tr = document.createElement('tr');
    tr.className = 'new-item-row table-primary';
    tr.setAttribute('data-menu-item-id', resolved.id);
    tr.setAttribute('data-temp-id', tempIdCounter--);
    const subtotal = resolved.price * qty;
    tr.innerHTML = `
      <td class="text-center"><input type="checkbox" class="fire-select" disabled /></td>
      <td><div class="fw-semibold">${resolved.displayName || name}</div><input type="text" class="form-control form-control-sm item-note mt-1" placeholder="Note" /></td>
      <td class="text-center"><input type="number" class="form-control form-control-sm item-qty" value="${qty}" min="1" /></td>
      <td class="text-end">${formatMoney(resolved.price)}</td>
      <td class="text-end subtotal-cell">${formatMoney(subtotal)}</td>
      <td><span class="badge bg-info text-dark">New</span></td>
      <td class="text-end"><button type="button" class="btn btn-outline-danger btn-sm remove-new" aria-label="Remove"><i class="fas fa-times"></i></button></td>`;
    tbody.appendChild(tr);
    attachRowEvents(tr);
    recalcTotals();
  }

  function toast(msg,type){
    if(window.toastr){
      if(type==='error') toastr.error(msg); else if(type==='success') toastr.success(msg); else toastr.info(msg);
    } else {
      console[type==='error'?'error':'log']('[Toast]',msg);
    }
  }

  if(addForm){
    addForm.addEventListener('submit', e=>{
      e.preventDefault();
      const name = menuInput.value.trim();
      const qty = parseInt(qtyInput.value,10)||1;
      if(!name){toast('Enter a menu item','error');return;}
      addNewItem(name, qty);
      menuInput.value=''; qtyInput.value='1'; menuInput.focus();
    });
  }

  if(selectAllFire){
    selectAllFire.addEventListener('change', ()=>{
      const checked = selectAllFire.checked;
      tbody.querySelectorAll('.fire-select:not(:disabled)').forEach(cb=> cb.checked = checked);
    });
  }

  // Fire modal select all
  document.addEventListener('DOMContentLoaded', ()=>{
    const fireSelectAll = document.getElementById('fireSelectAll');
    if(fireSelectAll){
      fireSelectAll.addEventListener('change', ()=>{
        const checked = fireSelectAll.checked;
        document.querySelectorAll('.fire-item-checkbox').forEach(cb=> cb.checked = checked);
      });
    }
  });

  async function saveOrder(){
    const payload = collectPayload();
    console.log('Save order called. Payload:', payload);
    console.log('Order ID:', orderId);
    
    if(!payload.length){ 
      toast('No items to save','error'); 
      console.log('No items in payload');
      return; 
    }
    
    saveBtn.disabled = true; 
    const original = saveBtn.innerHTML; 
    saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Saving...';
    
    try{
      console.log('Making fetch request to:', `/Order/UpdateMultipleOrderItems?orderId=${orderId}`);
      const antiForgery = getAntiForgery();
      if(!antiForgery){
        console.warn('Anti-forgery token missing');
      }
      const res = await fetch(`/Order/UpdateMultipleOrderItems?orderId=${orderId}`,{
        method:'POST',
        headers: {
          'Content-Type':'application/json',
          'RequestVerificationToken': antiForgery
        },
        body: JSON.stringify(payload)
      });
      
      console.log('Response status:', res.status);
      console.log('Response headers:', res.headers);
      
      if (!res.ok) {
        throw new Error(`HTTP error! status: ${res.status}`);
      }
      
      const data = await res.json().catch((parseError)=>{
        console.error('JSON parse error:', parseError);
        return {success:false,message:'Invalid server response - could not parse JSON'};
      });
      
      console.log('Response data:', data);
      
      if(!data.success){ 
        throw new Error(data.message||'Save failed'); 
      }
      
      toast('Order saved','success');
      // Reload to get authoritative data (ensures IDs for new rows)
      setTimeout(()=>{ window.location.reload(); }, 600);
    }catch(err){
      console.error('Save order error:', err);
      toast(err.message||'Error saving order','error');
      saveBtn.disabled=false; 
      saveBtn.innerHTML = original;
    }
  }

  if(saveBtn){
    saveBtn.addEventListener('click', saveOrder);
  }

  // Attach events for existing rows
  tbody.querySelectorAll('tr').forEach(tr=> attachRowEvents(tr));
})();

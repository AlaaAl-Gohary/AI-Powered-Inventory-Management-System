let rowIndex = 0;

function buildProductOptions() {
    let options = '<option value="">-- اختار منتج --</option>';
    products.forEach(p => {
        options += `<option value="${p.productID}" data-price="${p.unitPrice}" data-stock="${p.stockQuantity}">${p.productName} (متاح: ${p.stockQuantity})</option>`;
    });
    return options;
}

function addRow() {
    const tbody = document.querySelector('#itemsTable tbody');
    const index = rowIndex++;

    const row = document.createElement('tr');
    row.innerHTML = `
        <td>
            <select class="form-select product-select" name="Items[${index}].ProductID">
                ${buildProductOptions()}
            </select>
        </td>
        <td>
            <input type="number" class="form-control qty-input" name="Items[${index}].Quantity" min="1" value="1" />
            <small class="text-danger stock-warning d-none">الكمية أكبر من المتاح بالمخزون</small>
        </td>
        <td>
            <input type="number" step="0.01" class="form-control price-input" name="Items[${index}].UnitPrice" readonly />
        </td>
        <td class="line-total">0.00</td>
        <td class="stock-cell">-</td>
        <td><button type="button" class="btn btn-danger btn-sm remove-row">حذف</button></td>
    `;
    tbody.appendChild(row);
    attachRowEvents(row);
}

function attachRowEvents(row) {
    const select = row.querySelector('.product-select');
    const qtyInput = row.querySelector('.qty-input');
    const priceInput = row.querySelector('.price-input');
    const stockCell = row.querySelector('.stock-cell');
    const stockWarning = row.querySelector('.stock-warning');
    const removeBtn = row.querySelector('.remove-row');

    function currentStock() {
        const opt = select.selectedOptions[0];
        return opt ? parseInt(opt.dataset.stock || '0', 10) : 0;
    }

    function validateQty() {
        const stock = currentStock();
        const qty = parseInt(qtyInput.value || '0', 10);
        const invalid = qty > stock || qty <= 0;
        stockWarning.classList.toggle('d-none', !invalid);
        qtyInput.classList.toggle('is-invalid', invalid);
        return !invalid;
    }

    function updateRowTotal() {
        const qty = parseFloat(qtyInput.value || '0');
        const price = parseFloat(priceInput.value || '0');
        row.querySelector('.line-total').textContent = (qty * price).toFixed(2);
        updateGrandTotal();
    }

    select.addEventListener('change', () => {
        const opt = select.selectedOptions[0];
        const price = opt ? (opt.dataset.price || 0) : 0;
        const stock = opt ? (opt.dataset.stock || 0) : 0;
        priceInput.value = parseFloat(price).toFixed(2);
        stockCell.textContent = stock;
        qtyInput.max = stock;
        validateQty();
        updateRowTotal();
    });

    qtyInput.addEventListener('input', () => {
        validateQty();
        updateRowTotal();
    });

    removeBtn.addEventListener('click', () => {
        row.remove();
        updateGrandTotal();
    });
}

function updateGrandTotal() {
    let total = 0;
    document.querySelectorAll('.line-total').forEach(cell => {
        total += parseFloat(cell.textContent || '0');
    });
    document.getElementById('grandTotal').textContent = total.toFixed(2);
}

document.getElementById('addItemBtn').addEventListener('click', addRow);

// نبدأ بصف واحد افتراضي عند فتح الصفحة
document.addEventListener('DOMContentLoaded', () => addRow());

// حماية إضافية: منع الإرسال لو فيه أي صف كميته أكبر من المتاح أو فاضي
document.getElementById('saleForm').addEventListener('submit', function (e) {
    const invalidRows = document.querySelectorAll('.qty-input.is-invalid');
    const emptyProducts = document.querySelectorAll('.product-select');
    let hasEmptyProduct = false;
    emptyProducts.forEach(sel => { if (!sel.value) hasEmptyProduct = true; });

    if (invalidRows.length > 0) {
        e.preventDefault();
        alert('في صنف الكمية بتاعته أكبر من المتاح بالمخزون، من فضلك عدّل الكمية قبل التأكيد');
        return;
    }
    if (hasEmptyProduct) {
        e.preventDefault();
        alert('في سطر لسه مافيهوش منتج مختار');
    }
});

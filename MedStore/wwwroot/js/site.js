function togglePassword(){const input=document.querySelector('input[name="Password"]');const button=document.querySelector('.show-password');if(!input||!button)return;const show=input.type==="password";input.type=show?"text":"password";button.textContent=show?"Hide":"Show";}

function addTransactionLine(){
  const container=document.getElementById('transaction-lines');
  if(!container)return;
  const template=container.querySelector('.transaction-line');
  if(!template)return;
  const row=template.cloneNode(true);
  const index=container.querySelectorAll('.transaction-line').length;
  row.querySelectorAll('[name]').forEach(el=>{el.name=el.name.replace(/Lines\[\d+\]/,`Lines[${index}]`);if(el.tagName==='SELECT')el.value='';else if(el.type==='number')el.value='1';});
  container.appendChild(row);
  reindexTransactionLines();
  updateCartTotals();
}

function removeTransactionLine(button){
  const row=button.closest('.transaction-line');
  const container=document.getElementById('transaction-lines');
  if(!row||!container)return;
  if(container.querySelectorAll('.transaction-line').length<=1){row.querySelector('select').value='';row.querySelector('input[type=number]').value='1';}
  else row.remove();
  reindexTransactionLines();
  updateCartTotals();
}

function reindexTransactionLines(){document.querySelectorAll('#transaction-lines .transaction-line').forEach((row,index)=>row.querySelectorAll('[name]').forEach(el=>el.name=el.name.replace(/Lines\[\d+\]/,`Lines[${index}]`)));}

function updateCartTotals(){
  const form=document.getElementById('transaction-form');if(!form)return;
  let subtotal=0;
  form.querySelectorAll('.transaction-line').forEach(row=>{const select=row.querySelector('select');const quantity=Number(row.querySelector('input[type=number]')?.value||0);subtotal+=Number(select?.selectedOptions[0]?.dataset.price||0)*quantity;});
  const discount=Math.min(subtotal,Math.max(0,Number(form.querySelector('[name="Discount"]')?.value||0)));
  const money=v=>'₹'+v.toFixed(2);
  const a=document.getElementById('cart-subtotal'),b=document.getElementById('cart-discount'),c=document.getElementById('cart-total');
  if(a)a.textContent=money(subtotal);if(b)b.textContent=money(discount);if(c)c.textContent=money(subtotal-discount);
}

document.addEventListener('input',event=>{if(event.target.closest('#transaction-form'))updateCartTotals();});
document.addEventListener('change',event=>{if(event.target.closest('#transaction-form'))updateCartTotals();});
document.addEventListener('DOMContentLoaded',updateCartTotals);

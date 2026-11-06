using System.Text.Json;
using MedStore.Models;
using Microsoft.AspNetCore.Identity;

namespace MedStore.Data;

public sealed class DemoStore
{
    private readonly object _gate = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private List<BusinessRecord> _categories = [];
    private List<BusinessRecord> _suppliers = [];
    private List<BusinessRecord> _customers = [];
    private List<BusinessRecord> _employees = [];
    private List<BusinessRecord> _expenses = [];
    private List<BusinessRecord> _leaves = [];
    private List<BusinessRecord> _feedback = [];
    private List<BusinessRecord> _settings = [];
    private List<StaffAccount> _users = [];
    private List<Medicine> _medicines = [];
    private List<StoreTransaction> _purchases = [];
    private List<StoreTransaction> _sales = [];
    private List<BusinessRecord> _payroll = [];
    private List<MovementRecord> _movements = [];
    private int _nextId = 1;

    public DemoStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "medistock-demo.json");
        if (File.Exists(_filePath))
        {
            try { Load(File.ReadAllText(_filePath)); }
            catch (JsonException) { File.Move(_filePath, _filePath + ".invalid-" + DateTime.Now.ToString("yyyyMMddHHmmss")); }
        }
        if (_categories.Count == 0) Seed();
        if (_users.Count == 0) SeedAdmin();
        Persist();
    }

    public Task<(string Name, string Role)?> Authenticate(string username, string password)
    {
        lock (_gate)
        {
            var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.IsActive);
            if (user is null) return Task.FromResult<(string, string)?>(null);
            var verified = new PasswordHasher<string>().VerifyHashedPassword(user.Username, user.PasswordHash, password);
            return Task.FromResult<(string, string)?>(verified == PasswordVerificationResult.Failed ? null : (user.FullName, user.Role));
        }
    }

    public Task<DashboardViewModel> Dashboard()
    {
        lock (_gate)
        {
            var today = DateTime.Today;
            return Task.FromResult(new DashboardViewModel
            {
                Medicines = _medicines.Count, Categories = _categories.Count, Suppliers = _suppliers.Count,
                Customers = _customers.Count, Employees = _employees.Count,
                LowStock = _medicines.Count(m => m.Quantity <= 10), Expired = _medicines.Count(m => m.ExpiryDate.Date < today),
                TodaySales = _sales.Where(s => s.Status == "Completed" && s.Date.Date == today).Sum(s => s.Total),
                MonthRevenue = _sales.Where(s => s.Status == "Completed" && s.Date.Month == today.Month && s.Date.Year == today.Year).Sum(s => s.Total),
                InventoryValue = _medicines.Sum(m => m.Quantity * m.PurchasePrice),
                Alerts = _medicines.Where(m => m.Quantity <= 10 || m.ExpiryDate.Date <= today.AddDays(60)).OrderBy(m => m.Quantity).ThenBy(m => m.ExpiryDate).Take(8).Select(Clone).ToList(),
                SalesLast7Days = Enumerable.Range(0,7).Select(offset=>today.AddDays(offset-6)).Select(day=>new DailySales(day,_sales.Where(s=>s.Status=="Completed"&&s.Date.Date==day).Sum(s=>s.Total))).ToList(),
                RecentSales = _sales.Where(s=>s.Status=="Completed").OrderByDescending(s=>s.Date).Take(5).Select(Clone).ToList()
            });
        }
    }

    public Task<List<Medicine>> Medicines(string? search = null)
    {
        lock (_gate) return Task.FromResult(_medicines.Where(m => string.IsNullOrWhiteSpace(search) || m.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || m.BatchNumber.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Name).Select(Clone).ToList());
    }

    public Task SaveMedicine(Medicine medicine)
    {
        lock (_gate)
        {
            var category = _categories.FirstOrDefault(c => c.Id == medicine.CategoryId) ?? throw new InvalidOperationException("Select a valid category.");
            if (_medicines.Any(m => m.BatchNumber.Equals(medicine.BatchNumber, StringComparison.OrdinalIgnoreCase) && m.Id != medicine.Id)) throw new InvalidOperationException("That batch number is already in use.");
            var isNew=medicine.Id==0;var stockChange=medicine.Quantity;
            if (isNew) { medicine.Id = _nextId++; _medicines.Add(Clone(medicine)); }
            else { var old = _medicines.FirstOrDefault(m => m.Id == medicine.Id) ?? throw new KeyNotFoundException(); stockChange=medicine.Quantity-old.Quantity;Copy(medicine, old); }
            _medicines.First(m => m.Id == medicine.Id).CategoryName = category.Name;
            if(stockChange!=0)SaveMovement(medicine.Id,isNew?"Opening stock":"Manual stock edit",stockChange,isNew?"Initial batch quantity":"Quantity changed while editing medicine");Persist();
        }
        return Task.CompletedTask;
    }
    public Task<Medicine?> Medicine(int id) { lock (_gate) return Task.FromResult(_medicines.FirstOrDefault(m => m.Id == id) is { } m ? Clone(m) : null); }
    public Task DeleteMedicine(int id)
    {
        lock (_gate)
        {
            if (_sales.Any(s => s.Lines.Any(l => l.MedicineId == id)) || _purchases.Any(p => p.Lines.Any(l => l.MedicineId == id)) || _movements.Any(m=>m.MedicineId==id)) throw new InvalidOperationException("This batch has stock history; keep the record and adjust its stock instead.");
            _medicines.RemoveAll(m => m.Id == id); Persist();
        }
        return Task.CompletedTask;
    }
    public Task<List<(int Id, string Name)>> Categories() { lock (_gate) return Task.FromResult(_categories.Select(c => (c.Id, c.Name)).ToList()); }

    public static IReadOnlyDictionary<string, (string Title, string Description, string Role, ModuleField[] Fields)> ModuleDefinitions { get; } = new Dictionary<string, (string, string, string, ModuleField[])>(StringComparer.OrdinalIgnoreCase)
    {
        ["categories"]=("Medicine categories","Group medicines for clear browsing.","Admin,Pharmacist",[new("Name","Category name","text",true),new("Description","Description")]),
        ["suppliers"]=("Suppliers","Supplier contacts and purchase partners.","Admin,Pharmacist",[new("Name","Supplier name","text",true),new("Company","Company name"),new("Contact","Contact number"),new("Email","Email","email"),new("Address","Address")]),
        ["customers"]=("Customers","Customer details and purchase history.","Admin,Pharmacist,Billing Staff",[new("Name","Customer name","text",true),new("Contact","Contact number"),new("Email","Email","email"),new("Address","Address")]),
        ["employees"]=("Employees","Employee records and salary details.","Admin",[new("Name","Employee name","text",true),new("Contact","Contact number"),new("Email","Email","email"),new("Address","Address"),new("Designation","Designation"),new("Amount","Basic monthly salary","number",true),new("Allowances","Monthly allowances","number"),new("Deductions","Monthly deductions","number"),new("Date","Joining date","date",true)]),
        ["expenses"]=("Expenses","Track store operating expenses.","Admin",[new("Name","Expense title","text",true),new("Category","Expense category","text",true),new("Amount","Amount","number",true),new("Date","Expense date","date",true),new("Description","Notes")]),
        ["feedback"]=("Feedback","Review customer feedback.","Admin,Pharmacist",[new("Name","Customer name"),new("Rating","Rating 1–5","number",true),new("Description","Comments")]),
        ["leaves"]=("Leave management","Apply for leave and review requests.","Admin,Pharmacist,Billing Staff",[new("Name","Employee name","text",true),new("StartDate","Start date","date",true),new("EndDate","End date","date",true),new("Description","Reason","text",true)]),
        ["settings"]=("Store settings","Set the store name, address, and contact details.","Admin",[new("Name","Store name","text",true),new("Address","Store address"),new("Contact","Contact number"),new("Email","Email","email")]),
        ["users"]=("User management","Manage staff accounts and access roles.","Admin",[new("Name","Full name","text",true),new("Username","Username","text",true),new("Role","Role","select",true),new("Description","New password","password")]),
        ["payroll"]=("Employee payroll","Generate and review monthly salary slips.","Admin",[new("Date","Payroll month","date",true)])
    };

    public Task<ModulePageViewModel> ModulePage(string module, string? search = null)
    {
        lock (_gate)
        {
            if (!ModuleDefinitions.TryGetValue(module, out var definition)) throw new KeyNotFoundException();
            var rows = module.Equals("users",StringComparison.OrdinalIgnoreCase)
                ? _users.Select(u=>new BusinessRecord{Id=u.Id,Name=u.FullName,Username=u.Username,Role=u.Role,Status=u.IsActive?"Active":"Inactive"}).ToList()
                : GetRecords(module);
            if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || (r.Category?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) || (r.Contact?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            return Task.FromResult(new ModulePageViewModel { Key=module.ToLowerInvariant(), Title=definition.Title, Description=definition.Description, Fields=definition.Fields.ToList(), Rows=rows.Select(Clone).OrderBy(r => r.Id).ToList(), CanEdit=true, IsLeave=module.Equals("leaves",StringComparison.OrdinalIgnoreCase) });
        }
    }
    public Task<BusinessRecord?> Record(string module, int id)
    {
        lock (_gate)
        {
            if(module.Equals("users",StringComparison.OrdinalIgnoreCase))
            { var u=_users.FirstOrDefault(x=>x.Id==id); return Task.FromResult(u is null?null:new BusinessRecord{Id=id,Name=u.FullName,Username=u.Username,Role=u.Role,Status=u.IsActive?"Active":"Inactive"}); }
            return Task.FromResult(GetRecords(module).FirstOrDefault(r => r.Id == id) is { } r ? Clone(r) : null);
        }
    }
    public Task SaveRecord(string module, BusinessRecord record)
    {
        lock (_gate)
        {
            if(module.Equals("users",StringComparison.OrdinalIgnoreCase))
            {
                if(string.IsNullOrWhiteSpace(record.Username)) throw new InvalidOperationException("Username is required.");
                if(!new[]{"Admin","Pharmacist","Billing Staff"}.Contains(record.Role)) throw new InvalidOperationException("Choose a supported role.");
                if(_users.Any(u=>u.Username.Equals(record.Username,StringComparison.OrdinalIgnoreCase)&&u.Id!=record.Id)) throw new InvalidOperationException("That username is already in use.");
                var existing=record.Id>0?_users.FirstOrDefault(u=>u.Id==record.Id):null;
                if(existing is null)
                {
                    if(string.IsNullOrWhiteSpace(record.Description)||record.Description.Length<8) throw new InvalidOperationException("New passwords must have at least 8 characters.");
                    var user=new StaffAccount{Id=_nextId++,Username=record.Username,FullName=record.Name,Role=record.Role,IsActive=true};user.PasswordHash=new PasswordHasher<string>().HashPassword(user.Username,record.Description);_users.Add(user);
                }
                else
                {
                    if(existing.IsActive&&existing.Role=="Admin"&&(record.Role!="Admin"||record.Status=="Inactive")&&_users.Count(u=>u.IsActive&&u.Role=="Admin")<=1)throw new InvalidOperationException("Keep at least one active administrator.");
                    existing.FullName=record.Name;existing.Role=record.Role;existing.Username=record.Username;existing.IsActive=record.Status!="Inactive";
                    if(!string.IsNullOrWhiteSpace(record.Description)) existing.PasswordHash=new PasswordHasher<string>().HashPassword(existing.Username,record.Description);
                }
                Persist();return Task.CompletedTask;
            }
            if(string.IsNullOrWhiteSpace(record.Name) && !module.Equals("settings",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Name or title is required.");
            if(record.Amount<0||record.Allowances<0||record.Deductions<0) throw new InvalidOperationException("Amounts cannot be negative.");
            if(module.Equals("expenses",StringComparison.OrdinalIgnoreCase)&&record.Amount<=0) throw new InvalidOperationException("Expense amount must be greater than zero.");
            if(module.Equals("feedback",StringComparison.OrdinalIgnoreCase)&&(record.Rating<1||record.Rating>5)) throw new InvalidOperationException("Rating must be between 1 and 5.");
            if(module.Equals("leaves",StringComparison.OrdinalIgnoreCase)&&record.EndDate.Date<record.StartDate.Date) throw new InvalidOperationException("Leave end date must be on or after its start date.");
            if(module.Equals("leaves",StringComparison.OrdinalIgnoreCase)&&string.IsNullOrWhiteSpace(record.Description))throw new InvalidOperationException("Enter a reason for leave.");
            if(module.Equals("employees",StringComparison.OrdinalIgnoreCase)&&record.Amount<=0)throw new InvalidOperationException("Salary must be greater than zero.");
            var rows=GetRecords(module);
            if (module.Equals("categories",StringComparison.OrdinalIgnoreCase) && rows.Any(r=>r.Name.Equals(record.Name,StringComparison.OrdinalIgnoreCase)&&r.Id!=record.Id)) throw new InvalidOperationException("A category with that name already exists.");
            if(module.Equals("settings",StringComparison.OrdinalIgnoreCase)&&rows.Count>0&&record.Id==0){record.Id=rows[0].Id;record.Status=rows[0].Status;Copy(record,rows[0]);}
            else if (record.Id==0) { record.Id=_nextId++; if(module.Equals("leaves",StringComparison.OrdinalIgnoreCase)) record.Status="Pending"; rows.Add(Clone(record)); }
            else { var old=rows.FirstOrDefault(r=>r.Id==record.Id)??throw new KeyNotFoundException(); Copy(record,old); }
            Persist();
        }
        return Task.CompletedTask;
    }
    public Task DeleteRecord(string module, int id)
    {
        lock (_gate)
        {
            if(module.Equals("users",StringComparison.OrdinalIgnoreCase)){if(_users.Count(u=>u.Role=="Admin"&&u.IsActive)<=1&&_users.FirstOrDefault(u=>u.Id==id)?.Role=="Admin")throw new InvalidOperationException("Keep at least one active administrator.");_users.RemoveAll(u=>u.Id==id);Persist();return Task.CompletedTask;}
            if(module.Equals("categories",StringComparison.OrdinalIgnoreCase) && _medicines.Any(m=>m.CategoryId==id)) throw new InvalidOperationException("Reassign or remove medicines in this category first.");
            if(module.Equals("suppliers",StringComparison.OrdinalIgnoreCase)&&_purchases.Any(t=>t.PartnerName==GetRecords(module).FirstOrDefault(r=>r.Id==id)?.Name))throw new InvalidOperationException("This supplier has purchase history and cannot be deleted.");
            if(module.Equals("customers",StringComparison.OrdinalIgnoreCase)&&_sales.Any(t=>t.PartnerName==GetRecords(module).FirstOrDefault(r=>r.Id==id)?.Name))throw new InvalidOperationException("This customer has sales history and cannot be deleted.");
            GetRecords(module).RemoveAll(r=>r.Id==id); Persist();
        }
        return Task.CompletedTask;
    }
    public Task SetLeaveStatus(int id, string status)
    {
        lock (_gate) { var r=_leaves.FirstOrDefault(x=>x.Id==id)??throw new KeyNotFoundException(); r.Status=status; Persist(); }
        return Task.CompletedTask;
    }
    public Task<LeaveCalendarViewModel> LeaveCalendar(DateTime month)
    {
        lock(_gate)
        {
            var first=new DateTime(month.Year,month.Month,1);var start=first.AddDays(-(int)first.DayOfWeek);
            return Task.FromResult(new LeaveCalendarViewModel{Month=first,Days=Enumerable.Range(0,42).Select(i=>start.AddDays(i)).ToList(),Leaves=_leaves.Select(Clone).ToList()});
        }
    }

    public Task<TransactionPageViewModel> TransactionPage(string kind,string? search=null)
    {
        lock (_gate) return Task.FromResult(new TransactionPageViewModel { Kind=kind, Transactions=(kind=="purchase"?_purchases:_sales).Where(t=>string.IsNullOrWhiteSpace(search)||t.InvoiceNumber.Contains(search,StringComparison.OrdinalIgnoreCase)||t.PartnerName.Contains(search,StringComparison.OrdinalIgnoreCase)||t.Lines.Any(l=>l.MedicineName.Contains(search,StringComparison.OrdinalIgnoreCase))).Select(Clone).OrderByDescending(t=>t.Date).ToList(), Medicines=_medicines.Select(Clone).ToList(), Partners=(kind=="purchase"?_suppliers:_customers).Select(Clone).ToList() });
    }
    public Task<InvoiceViewModel?> Invoice(string kind,int id)
    {
        lock(_gate)
        {
            var tx=(kind=="purchase"?_purchases:_sales).FirstOrDefault(t=>t.Id==id);
            if(tx is null)return Task.FromResult<InvoiceViewModel?>(null);
            var info=_settings.FirstOrDefault();
            return Task.FromResult<InvoiceViewModel?>(new InvoiceViewModel{Transaction=Clone(tx),IsPurchase=kind=="purchase",StoreName=info?.Name??"MediStock Medical Store",StoreAddress=info?.Address??"",StoreContact=info?.Contact??"",StoreEmail=info?.Email??""});
        }
    }
    public Task CreateTransaction(TransactionInput input)
    {
        lock (_gate)
        {
            bool purchase=input.Kind.Equals("purchase",StringComparison.OrdinalIgnoreCase);
            var lines=input.Lines.Where(x=>x.MedicineId>0&&x.Quantity>0).GroupBy(x=>x.MedicineId).Select(g=>new TransactionLineInput{MedicineId=g.Key,Quantity=g.Sum(x=>x.Quantity)}).ToList();
            if(lines.Count==0) throw new InvalidOperationException("Add at least one medicine to the transaction.");
            if(purchase&&(input.PartnerId is null||!_suppliers.Any(s=>s.Id==input.PartnerId)))throw new InvalidOperationException("Choose a supplier for this purchase.");
            if(!purchase && input.Discount<0) throw new InvalidOperationException("Discount cannot be negative.");
            var partner=(purchase?_suppliers:_customers).FirstOrDefault(x=>x.Id==input.PartnerId);
            if(input.PartnerId.HasValue&&partner is null)throw new InvalidOperationException("The selected customer or supplier could not be found.");
            var transaction=new StoreTransaction { Id=_nextId++, InvoiceNumber=(purchase?"PUR":"INV")+DateTime.Now.ToString("yyyyMMdd")+_nextId.ToString("D4"), PartnerName=partner?.Name??(purchase?"Supplier":"Walk-in customer"),PartnerContact=partner?.Contact,PartnerAddress=partner?.Address };
            foreach(var requested in lines)
            {
                var medicine=_medicines.FirstOrDefault(x=>x.Id==requested.MedicineId)??throw new InvalidOperationException("A selected medicine is unavailable.");
                if(purchase&&medicine.ExpiryDate.Date<DateTime.Today)throw new InvalidOperationException($"Cannot receive expired batch {medicine.BatchNumber}.");
                if(!purchase && medicine.ExpiryDate.Date<DateTime.Today) throw new InvalidOperationException($"{medicine.Name} has expired and cannot be sold.");
                if(!purchase && medicine.Quantity<requested.Quantity) throw new InvalidOperationException($"Only {medicine.Quantity} units of {medicine.Name} are available.");
                var unit=purchase?medicine.PurchasePrice:medicine.SellingPrice;
                transaction.Lines.Add(new TransactionLine { MedicineId=medicine.Id,MedicineName=medicine.Name,BatchNumber=medicine.BatchNumber,Quantity=requested.Quantity,UnitPrice=unit,Total=unit*requested.Quantity });
            }
            transaction.Subtotal=transaction.Lines.Sum(x=>x.Total);
            if(input.Discount>transaction.Subtotal) throw new InvalidOperationException("Discount cannot exceed the subtotal.");
            transaction.Discount=purchase?0:input.Discount; transaction.Total=transaction.Subtotal-transaction.Discount;
            foreach(var line in transaction.Lines)
            {
                var medicine=_medicines.First(x=>x.Id==line.MedicineId); var delta=purchase?line.Quantity:-line.Quantity;
                medicine.Quantity+=delta; SaveMovement(medicine.Id,purchase?"Purchase":"Sale",delta,transaction.InvoiceNumber);
            }
            (purchase?_purchases:_sales).Add(transaction); Persist();
        }
        return Task.CompletedTask;
    }
    public Task CancelSale(int id)
    {
        lock (_gate)
        {
            var sale=_sales.FirstOrDefault(s=>s.Id==id)??throw new KeyNotFoundException(); if(sale.Status!="Completed")throw new InvalidOperationException("This sale is already cancelled.");
            foreach(var line in sale.Lines){var med=_medicines.FirstOrDefault(m=>m.Id==line.MedicineId);if(med is not null){med.Quantity+=line.Quantity;SaveMovement(med.Id,"Sale reversal",line.Quantity,sale.InvoiceNumber);}}
            sale.Status="Cancelled"; Persist();
        }
        return Task.CompletedTask;
    }
    public Task GeneratePayroll(DateTime period)
    {
        lock (_gate)
        {
            if(period.Year<2000||period.Year>DateTime.Today.Year+1)throw new InvalidOperationException("Choose a valid payroll month.");
            var policy=(IPayrollPolicy)new StandardPayrollPolicy();
            var calculations=_employees.Where(e=>e.Status!="Inactive"&&!_payroll.Any(p=>p.Name==e.Name&&p.Date.Year==period.Year&&p.Date.Month==period.Month))
                .Select(e=>policy.Calculate(new SalariedEmployee(e.Id,e.Name,e.Amount,e.Allowances,e.Deductions),new DateTime(period.Year,period.Month,1))).ToList();
            foreach(var calculation in calculations)_payroll.Add(new BusinessRecord { Id=_nextId++,Name=calculation.EmployeeName,Date=calculation.Period,Amount=calculation.Net,Allowances=calculation.Allowances,Deductions=calculation.Deductions,Category="Salary slip",Description=$"Basic {calculation.Basic:C}; allowances {calculation.Allowances:C}; deductions {calculation.Deductions:C}; net {calculation.Net:C}",Status="Paid" });
            Persist();
        }
        return Task.CompletedTask;
    }
    public Task<List<BusinessRecord>> Payroll() { lock(_gate)return Task.FromResult(_payroll.Select(Clone).OrderByDescending(x=>x.Date).ToList()); }
    public Task<BusinessRecord?> PayrollSlip(int id){lock(_gate)return Task.FromResult(_payroll.FirstOrDefault(x=>x.Id==id) is{} r?Clone(r):null);}
    public Task<List<MovementRecord>> Movements() { lock(_gate)return Task.FromResult(_movements.Select(Clone).OrderByDescending(x=>x.Date).ToList()); }
    public Task AdjustStock(int medicineId,int change,string note)
    {
        lock(_gate){var med=_medicines.FirstOrDefault(m=>m.Id==medicineId)??throw new KeyNotFoundException();if(med.Quantity+change<0)throw new InvalidOperationException("Stock adjustment cannot make inventory negative.");med.Quantity+=change;SaveMovement(med.Id,"Adjustment",change,note);Persist();}
        return Task.CompletedTask;
    }
    public Task<List<StoreTransaction>> AllSales() { lock(_gate)return Task.FromResult(_sales.Select(Clone).OrderByDescending(s=>s.Date).ToList()); }

    private List<BusinessRecord> GetRecords(string module)=>module.ToLowerInvariant() switch
    {
        "categories"=>_categories,"suppliers"=>_suppliers,"customers"=>_customers,"employees"=>_employees,"expenses"=>_expenses,"leaves"=>_leaves,"feedback"=>_feedback,"settings"=>_settings,"payroll"=>_payroll,
        _=>throw new KeyNotFoundException()
    };
    private void Seed()
    {
        _categories=[new(){Id=1,Name="General",Description="General medicines"},new(){Id=2,Name="Pain relief",Description="Analgesics and pain relief"},new(){Id=3,Name="Antibiotics",Description="Prescription medicines"}];
        _suppliers=[new(){Id=4,Name="HealthCare Labs",Company="HealthCare Labs",Contact="9876500001",Email="supply@healthcare.test",Address="New Delhi"},new(){Id=5,Name="Northstar Pharma",Company="Northstar Pharma",Contact="9876500002",Email="orders@northstar.test",Address="Mumbai"}];
        _customers=[new(){Id=6,Name="Aarav Sharma",Contact="9876500101"},new(){Id=7,Name="Walk-in customer",Contact=""}];
        _employees=[new(){Id=8,Name="Riya Patel",Contact="9876500201",Email="riya@example.test",Designation="Pharmacist",Amount=28000,Date=DateTime.Today.AddYears(-2)},new(){Id=9,Name="Kabir Singh",Designation="Billing Staff",Amount=22000,Date=DateTime.Today.AddYears(-1)}];
        _medicines=[
            new(){Id=10,Name="Paracetamol 500 mg",CategoryId=2,CategoryName="Pain relief",Manufacturer="HealthCare Labs",BatchNumber="PCM-26041",SupplierId=4,PurchasePrice=18.50m,SellingPrice=25m,Quantity=42,ExpiryDate=DateTime.Today.AddMonths(14),Description="Tablet"},
            new(){Id=11,Name="Cetirizine 10 mg",CategoryId=1,CategoryName="General",Manufacturer="Northstar Pharma",BatchNumber="CET-26018",SupplierId=5,PurchasePrice=22m,SellingPrice=32m,Quantity=8,ExpiryDate=DateTime.Today.AddMonths(10),Description="Allergy relief"},
            new(){Id=12,Name="Amoxicillin 250 mg",CategoryId=3,CategoryName="Antibiotics",Manufacturer="HealthCare Labs",BatchNumber="AMX-25192",SupplierId=4,PurchasePrice=64m,SellingPrice=85m,Quantity=16,ExpiryDate=DateTime.Today.AddDays(38),Description="Use as prescribed"},
            new(){Id=13,Name="Vitamin C 500 mg",CategoryId=1,CategoryName="General",Manufacturer="Northstar Pharma",BatchNumber="VTC-26009",SupplierId=5,PurchasePrice=30m,SellingPrice=45m,Quantity=5,ExpiryDate=DateTime.Today.AddMonths(18),Description="Supplement"}
        ];
        _nextId=14;
    }
    private void SeedAdmin()
    {
        var username="admin";
        _users=[new StaffAccount { Id=_nextId++,Username=username,FullName="Store Administrator",Role="Admin",IsActive=true,PasswordHash=new PasswordHasher<string>().HashPassword(username,"Admin@123") }];
    }
    private void SaveMovement(int medicineId,string type,int change,string note){var med=_medicines.FirstOrDefault(m=>m.Id==medicineId);if(med is not null)_movements.Add(new MovementRecord{MedicineId=medicineId,MedicineName=med.Name,Type=type,Change=change,Note=note});}
    private void Persist()
    {
        var state=new DemoState{Categories=_categories,Suppliers=_suppliers,Customers=_customers,Employees=_employees,Expenses=_expenses,Leaves=_leaves,Feedback=_feedback,Settings=_settings,Users=_users,Medicines=_medicines,Purchases=_purchases,Sales=_sales,Payroll=_payroll,Movements=_movements,NextId=_nextId};
        var temp=_filePath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(state,_jsonOptions));File.Move(temp,_filePath,true);
    }
    private void Load(string json)
    {
        var s=JsonSerializer.Deserialize<DemoState>(json);if(s is null)return;
        _categories=s.Categories??[];_suppliers=s.Suppliers??[];_customers=s.Customers??[];_employees=s.Employees??[];_expenses=s.Expenses??[];_leaves=s.Leaves??[];_feedback=s.Feedback??[];_settings=s.Settings??[];_users=s.Users??[];_medicines=s.Medicines??[];_purchases=s.Purchases??[];_sales=s.Sales??[];_payroll=s.Payroll??[];_movements=s.Movements??[];_nextId=Math.Max(s.NextId,(_categories.Concat(_suppliers).Concat(_customers).Concat(_employees).Concat(_expenses).Concat(_leaves).Concat(_feedback).Concat(_settings).Concat(_payroll).Select(x=>x.Id).Concat(_medicines.Select(x=>x.Id)).DefaultIfEmpty().Max())+1);
    }
    private static Medicine Clone(Medicine m)=>new(){Id=m.Id,Name=m.Name,CategoryId=m.CategoryId,CategoryName=m.CategoryName,Manufacturer=m.Manufacturer,BatchNumber=m.BatchNumber,SupplierId=m.SupplierId,PurchasePrice=m.PurchasePrice,SellingPrice=m.SellingPrice,Quantity=m.Quantity,ExpiryDate=m.ExpiryDate,Description=m.Description};
    private static BusinessRecord Clone(BusinessRecord r)=>(BusinessRecord)JsonSerializer.Deserialize(JsonSerializer.Serialize(r),typeof(BusinessRecord))!;
    private static StoreTransaction Clone(StoreTransaction t)=>(StoreTransaction)JsonSerializer.Deserialize(JsonSerializer.Serialize(t),typeof(StoreTransaction))!;
    private static MovementRecord Clone(MovementRecord m)=>(MovementRecord)JsonSerializer.Deserialize(JsonSerializer.Serialize(m),typeof(MovementRecord))!;
    private static void Copy(Medicine a,Medicine b){b.Name=a.Name;b.CategoryId=a.CategoryId;b.CategoryName=a.CategoryName;b.Manufacturer=a.Manufacturer;b.BatchNumber=a.BatchNumber;b.SupplierId=a.SupplierId;b.PurchasePrice=a.PurchasePrice;b.SellingPrice=a.SellingPrice;b.Quantity=a.Quantity;b.ExpiryDate=a.ExpiryDate;b.Description=a.Description;}
    private static void Copy(BusinessRecord a,BusinessRecord b){b.Name=a.Name;b.Contact=a.Contact;b.Email=a.Email;b.Address=a.Address;b.Company=a.Company;b.Designation=a.Designation;b.Amount=a.Amount;b.Allowances=a.Allowances;b.Deductions=a.Deductions;b.Date=a.Date;b.StartDate=a.StartDate;b.EndDate=a.EndDate;b.Category=a.Category;b.Description=a.Description;b.Status=a.Status;b.Role=a.Role;b.Rating=a.Rating;}
}

public sealed class StaffAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "Billing Staff";
    public bool IsActive { get; set; }
    public string PasswordHash { get; set; } = "";
}

public sealed class DemoState
{
    public List<BusinessRecord>? Categories { get; set; } = [];
    public List<BusinessRecord>? Suppliers { get; set; } = [];
    public List<BusinessRecord>? Customers { get; set; } = [];
    public List<BusinessRecord>? Employees { get; set; } = [];
    public List<BusinessRecord>? Expenses { get; set; } = [];
    public List<BusinessRecord>? Leaves { get; set; } = [];
    public List<BusinessRecord>? Feedback { get; set; } = [];
    public List<BusinessRecord>? Settings { get; set; } = [];
    public List<StaffAccount>? Users { get; set; } = [];
    public List<Medicine>? Medicines { get; set; } = [];
    public List<StoreTransaction>? Purchases { get; set; } = [];
    public List<StoreTransaction>? Sales { get; set; } = [];
    public List<BusinessRecord>? Payroll { get; set; } = [];
    public List<MovementRecord>? Movements { get; set; } = [];
    public int NextId { get; set; } = 1;
}

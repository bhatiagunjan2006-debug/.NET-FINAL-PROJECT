namespace MedStore.Models;

public interface IPayrollPolicy
{
    PayrollCalculation Calculate(EmployeeBase employee, DateTime period);
}

public abstract class EmployeeBase
{
    protected EmployeeBase(int id,string name,decimal basicSalary,decimal allowances,decimal deductions)
    { Id=id;Name=name;BasicSalary=basicSalary;Allowances=allowances;Deductions=deductions; }
    public int Id { get; }
    public string Name { get; }
    public decimal BasicSalary { get; }
    public decimal Allowances { get; }
    public decimal Deductions { get; }
    public abstract decimal GrossSalary { get; }
}

public sealed class SalariedEmployee : EmployeeBase
{
    public SalariedEmployee(int id,string name,decimal basicSalary,decimal allowances,decimal deductions)
        :base(id,name,basicSalary,allowances,deductions) { }
    public override decimal GrossSalary => BasicSalary+Allowances;
}

public sealed class StandardPayrollPolicy : IPayrollPolicy
{
    public PayrollCalculation Calculate(EmployeeBase employee,DateTime period)
    {
        if(employee.BasicSalary<0||employee.Allowances<0||employee.Deductions<0)throw new InvalidOperationException("Payroll amounts cannot be negative.");
        if(employee.Deductions>employee.GrossSalary)throw new InvalidOperationException($"Deductions exceed gross salary for {employee.Name}.");
        return new PayrollCalculation(employee.Name,period,employee.BasicSalary,employee.Allowances,employee.Deductions,employee.GrossSalary-employee.Deductions);
    }
}

public sealed record PayrollCalculation(string EmployeeName,DateTime Period,decimal Basic,decimal Allowances,decimal Deductions,decimal Net);

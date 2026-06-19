using MediatR;
using Microsoft.EntityFrameworkCore;
using RBMS.Application.Common.Exceptions;
using RBMS.Application.Common.Interfaces;
using RBMS.Application.Common.Models;

namespace RBMS.Application.Features.Employees.Queries;

public record GetEmployeesQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<EmployeeListItemDto>>;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetEmployeesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<EmployeeListItemDto>> Handle(GetEmployeesQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Employees.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(e => e.FullName.ToLower().Contains(term)
                || e.EmployeeCode.ToLower().Contains(term)
                || e.Mobile.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * size).Take(size)
            .Select(e => new EmployeeListItemDto(
                e.Id, e.EmployeeCode, e.FullName, e.Designation, e.Mobile, e.Status, e.MonthlyCtc))
            .ToListAsync(ct);

        return new PagedResult<EmployeeListItemDto>(items, total, page, size);
    }
}

public record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeDto>;

public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    private readonly IApplicationDbContext _db;
    public GetEmployeeByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken ct)
    {
        var e = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Employee), request.Id);

        return new EmployeeDto(
            e.Id, e.EmployeeCode, e.FullName, e.Gender, e.DateOfBirth, e.Mobile, e.Email,
            e.AddressLine1, e.City, e.State, e.Pincode, e.EmergencyContactName, e.EmergencyContactPhone,
            e.Designation, e.Department, e.JoiningDate, e.ExitDate, e.Status, e.MonthlyCtc,
            e.BankName, e.Ifsc, e.AccountLast4);
    }
}

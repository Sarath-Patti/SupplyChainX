using Microsoft.EntityFrameworkCore;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ISupplyChainXDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        ISupplyChainXDbContext dbContext,
        IPasswordService passwordService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new DomainException("Username is required.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new DomainException("Password must be at least 6 characters long.");

        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _dbContext.Users
            .AnyAsync(u => u.Username == normalizedUsername || u.Email == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            throw new ConflictException("Username or Email is already registered.");
        }

        // Determine target role name
        string targetRoleName;
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var requestedRole = request.Role.Trim();
            if (string.Equals(requestedRole, Role.Admin, StringComparison.OrdinalIgnoreCase))
                targetRoleName = Role.Admin;
            else if (string.Equals(requestedRole, Role.Operator, StringComparison.OrdinalIgnoreCase))
                targetRoleName = Role.Operator;
            else if (string.Equals(requestedRole, Role.Viewer, StringComparison.OrdinalIgnoreCase))
                targetRoleName = Role.Viewer;
            else
                throw new DomainException("Invalid role specified. Allowed roles: Admin, Operator, Viewer.");
        }
        else
        {
            var hasAnyUsers = await _dbContext.Users.AnyAsync(cancellationToken);
            targetRoleName = hasAnyUsers ? Role.Viewer : Role.Admin;
        }

        // Dummy instance to compute hash
        var tempUser = new User(normalizedUsername, normalizedEmail, "temp_hash");
        var passwordHash = _passwordService.HashPassword(tempUser, request.Password);
        tempUser.UpdatePassword(passwordHash);

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName, cancellationToken);
        if (role == null)
        {
            role = new Role(targetRoleName, $"{targetRoleName} System Role");
            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _dbContext.Users.Add(tempUser);
        _dbContext.UserRoles.Add(new UserRole(tempUser.Id, role.Id));
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = new List<string> { role.Name };
        var token = _jwtTokenGenerator.GenerateToken(tempUser, roles);

        var userDto = new UserDto(
            tempUser.Id,
            tempUser.Username,
            tempUser.Email,
            roles,
            tempUser.IsActive,
            tempUser.CreatedAtUtc
        );

        return new AuthResponse(token, userDto);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new DomainException("Username and Password are required.");
        }

        var normalizedUsername = request.Username.Trim();
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername || u.Email == normalizedUsername.ToLowerInvariant(), cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!_passwordService.VerifyPassword(user, user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive. Please contact administrator.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var token = _jwtTokenGenerator.GenerateToken(user, roles);

        var userDto = new UserDto(
            user.Id,
            user.Username,
            user.Email,
            roles,
            user.IsActive,
            user.CreatedAtUtc
        );

        return new AuthResponse(token, userDto);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            roles,
            user.IsActive,
            user.CreatedAtUtc
        );
    }
}

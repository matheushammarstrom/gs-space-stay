namespace SpaceStay.Core.Common;

// Exceções de domínio. O middleware de erros da API traduz cada uma para o status
// HTTP correto (indicado ao lado), sem vazar detalhes internos.

public class NotFoundException(string message) : Exception(message);          // 404
public class ConflictException(string message) : Exception(message);          // 409
public class DomainValidationException(string message) : Exception(message);  // 400
public class AuthenticationException(string message) : Exception(message);    // 401
public class ForbiddenException(string message) : Exception(message);         // 403

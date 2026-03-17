namespace TaskOrchestrator.Shared.Contracts;

// Returned as HTTP 409 body. ServerSnapshot lets the client show a diff
// so the user can decide whether to overwrite or discard their changes.
public record ConflictResponse<T>(
    string Message,
    T ServerSnapshot
);

# Service Implementation Issues

This directory contains supporting reference material for implementing Intune Commander services. Service status and per-service checklists are tracked in [SERVICE-IMPLEMENTATION-PLAN.md](../SERVICE-IMPLEMENTATION-PLAN.md) and the [GitHub Issues](https://github.com/adamgell/IntuneCommander/issues) for this repository.

## Quick Reference

### Common Patterns Across All Services

Each service follows these conventions:

1. **Interface + Implementation** in `src/Intune.Commander.Core/Services/`
2. **Constructor** accepts `GraphServiceClient`
3. **Async methods** with `CancellationToken cancellationToken = default`
4. **List methods** use manual `@odata.nextLink` pagination with `$top=999`
5. **CRUD methods** throw on null create/update responses
6. **Assignment pattern** where supported:
   - `GetAssignmentsAsync(id)`
   - `Assign...Async(id, List<TAssignment>)`

### Implementation Phases (per service)

- **Phase A — Scaffold:** Add interfaces, service classes, method signatures
- **Phase B — Functional Completion:** Implement special helpers, normalization
- **Phase C — Desktop Integration:** Add to `MainWindowViewModel`, UI wiring
- **Phase D — Export/Import:** Extend `ExportService`/`ImportService`
- **Phase E — Tests:** Add unit tests in `Intune.Commander.Core.Tests`

### Definition of Done (per service)

- ✅ Compiles without errors
- ✅ Unit tests added and passing
- ✅ Manual pagination on all list methods
- ✅ CancellationToken passed to all Graph calls
- ✅ No UI-thread sync blocking
- ✅ Consumed by desktop ViewModel
- ✅ Export/Import functional

## Notes

- Service implementation details (method signatures, checklists, and status) are tracked in [SERVICE-IMPLEMENTATION-PLAN.md](../SERVICE-IMPLEMENTATION-PLAN.md) and the corresponding [GitHub Issues](https://github.com/adamgell/IntuneCommander/issues)
- Reference the main [SERVICE-IMPLEMENTATION-PLAN.md](../SERVICE-IMPLEMENTATION-PLAN.md) for architectural context
- Follow the conventions documented in [.github/copilot-instructions.md](../../.github/copilot-instructions.md)
- See [CLAUDE.md](../../CLAUDE.md) for overall design decisions

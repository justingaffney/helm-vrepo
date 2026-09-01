namespace VRepo.Cli.Handlers;

public interface IHandler<TParameters>
{
    ValueTask Handle(TParameters parameters, CancellationToken cancellationToken = default);
}

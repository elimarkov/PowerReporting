namespace PowerTrading.Reporting.Service.Services;

public interface IMapper<TFrom, TTo>
{
    TTo Map(TFrom source);
}
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Web.Client;

public partial class Routes : ComponentBase, IDisposable
{
    public void Dispose()
    {
        CustomStateContainer.OnChangedAsync -= OnChangedAsync;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            CustomStateContainer.OnChangedAsync += OnChangedAsync;
            await JsRuntime.InvokeVoidAsync("CloseProgressBar");
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnChangedAsync()
    {
        await InvokeAsync(StateHasChanged);
    }
}
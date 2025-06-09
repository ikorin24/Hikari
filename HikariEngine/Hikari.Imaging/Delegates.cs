#nullable enable
using Cysharp.Threading.Tasks;

namespace Hikari.Imaging
{
    public delegate void ImageAction(ImageViewMut image);
    public delegate void ImageAction<in TArg>(ImageViewMut image, TArg arg);
    public delegate void ReadOnlyImageAction(ImageView image);
    public delegate void ReadOnlyImageAction<in TArg>(ImageView image, TArg arg);

    public delegate TResult ImageFunc<out TResult>(ImageViewMut image);
    public delegate TResult ImageFunc<in TArg, out TResult>(ImageViewMut image, TArg arg);
    public delegate TResult ReadOnlyImageFunc<out TResult>(ImageView image);
    public delegate TResult ReadOnlyImageFunc<in TArg, out TResult>(ImageView image, TArg arg);

    public delegate UniTask AsyncImageAction(ImageViewMut image);
    public delegate UniTask AsyncImageAction<in TArg>(ImageViewMut image, TArg arg);
    public delegate UniTask AsyncReadOnlyImageAction(ImageView image);
    public delegate UniTask AsyncReadOnlyImageAction<in TArg>(ImageView image, TArg arg);

    public delegate UniTask<TResult> AsyncImageFunc<TResult>(ImageViewMut image);
    public delegate UniTask<TResult> AsyncImageFunc<in TArg, TResult>(ImageViewMut image, TArg arg);
    public delegate UniTask<TResult> AsyncReadOnlyImageFunc<TResult>(ImageView image);
    public delegate UniTask<TResult> AsyncReadOnlyImageFunc<in TArg, TResult>(ImageView image, TArg arg);
}

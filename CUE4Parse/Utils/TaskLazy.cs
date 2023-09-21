using System;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse.Utils;

#if FALSE

#else
public class TaskLazy<T>: Lazy<T> {
    public TaskLazy(Func<T> valueFactory) : base(valueFactory) {
    }
    public TaskLazy(T value) : base(value) {
    }
}
#endif
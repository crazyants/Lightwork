using System;
using System.Threading.Tasks;

namespace Lightwork.Core.Utilities
{
    public static class Monads
    {
        /// <summary>
        ///     Executes <c>evaluator</c> if <c>o</c> is not null and returns result of evaluation. 
        ///     Otherwise <c>null</c> is returned.
        /// </summary>
        public static TResult With<TInput, TResult>(this TInput o, Func<TInput, TResult> evaluator)
            where TResult : class
            where TInput : class
        {
            evaluator.NotNull();

            return o == null ? null : evaluator(o);
        }

        /// <summary>
        ///     Executes <c>evaluator</c> if <c>o</c> is not null and returns result of evaluation. 
        ///     Otherwise <c>null</c> is returned.
        /// </summary>
        public static TResult WithN<TInput, TResult>(this TInput? o, Func<TInput, TResult> evaluator)
            where TResult : class
            where TInput : struct
        {
            evaluator.NotNull();

            return o == null ? null : evaluator(o.Value);
        }

        /// <summary>
        ///     Executes <c>evaluator</c> if <c>o</c> is not null and returns result of evaluation. 
        ///     Otherwise <c>failureValue</c> is returned.
        /// </summary>
        public static TResult Return<TInput, TResult>(
            this TInput o, Func<TInput, TResult> evaluator, TResult failureValue = default(TResult))
            where TInput : class
        {
            evaluator.NotNull();

            return o == null ? failureValue : evaluator(o);
        }

        /// <summary>
        ///     Executes <c>evaluator</c> if <c>o</c> is not null and returns result of evaluation. 
        ///     Otherwise <c>failureValue</c> is returned.
        /// </summary>
        public static TResult ReturnN<TInput, TResult>(
            this TInput? o, Func<TInput, TResult> evaluator, TResult failureValue = default(TResult))
            where TInput : struct
        {
            evaluator.NotNull();

            return o == null ? failureValue : evaluator(o.Value);
        }

        /// <summary>
        ///     Executes <c>predicate</c> if <c>o</c> is not null and returns <c>o</c> if it evaluates to <c>true</c>. 
        ///     Otherwise <c>null</c> is returned.
        /// </summary>
        public static TInput If<TInput>(this TInput o, Predicate<TInput> predicate)
            where TInput : class
        {
            predicate.NotNull();

            return o == null ? null : (predicate(o) ? o : null);
        }

        /// <summary>
        ///     Executes <c>predicate</c> if <c>o</c> is not null and returns <c>o</c> if it evaluates to <c>true</c>. 
        ///     Otherwise <c>null</c> is returned.
        /// </summary>
        public static TInput? IfN<TInput>(this TInput? o, Predicate<TInput> predicate)
            where TInput : struct
        {
            predicate.NotNull();

            return o == null ? null : (predicate(o.Value) ? o : null);
        }

        /// <summary>
        ///     Executes <c>predicate</c> if <c>o</c> is not null and returns <c>o</c> if it evaluates to <c>false</c>. 
        ///     Otherwise <c>null</c> is returned.
        /// </summary>
        public static TInput Unless<TInput>(this TInput o, Predicate<TInput> predicate)
            where TInput : class
        {
            predicate.NotNull();

            return o == null ? null : (predicate(o) ? null : o);
        }

        /// <summary>
        ///     Executes <c>predicate</c> if <c>o</c> is not null and returns <c>o</c> if it evaluates to <c>false</c>. 
        ///     Otherwise <c>null</c> is returned.
        /// </summary>
        public static TInput? UnlessN<TInput>(this TInput? o, Predicate<TInput> predicate)
            where TInput : struct
        {
            predicate.NotNull();

            return o == null ? null : (predicate(o.Value) ? null : o);
        }

        /// <summary>
        ///     Executes <c>action</c> if <c>o</c> is not null and returns <c>o</c>. 
        ///     Otherwise <c>null</c> is returned.
        /// </summary>
        public static TInput Do<TInput>(this TInput o, Action<TInput> action) where TInput : class
        {
            action.NotNull();

            if (o == null)
            {
                return null;
            }

            action(o);
            return o;
        }

        /// <summary>
        ///     Executes async <c>action</c> if <c>o</c> is not null and returns resulting task.
        ///     Otherwise completed task is returned.
        /// </summary>
        public static Task Do<TInput>(this TInput o, Func<TInput, Task> action) where TInput : class
        {
            action.NotNull();

            return o == null ? Task.FromResult(0) : action(o);
        }

        /// <summary>
        ///     Executes <c>action</c> if <c>o</c> is not null and returns <c>o</c>. 
        ///     Otherwise <c>null</c> is returned.
        /// </summary>
        public static TInput? DoN<TInput>(this TInput? o, Action<TInput> action) where TInput : struct
        {
            action.NotNull();

            if (o == null)
            {
                return null;
            }

            action(o.Value);
            return o;
        }

        /// <summary>
        ///     Executes async <c>action</c> if <c>o</c> is not null and returns resulting task.
        ///     Otherwise completed task is returned.
        /// </summary>
        public static Task DoN<TInput>(this TInput? o, Func<TInput, Task> action) where TInput : struct
        {
            action.NotNull();

            return o == null ? Task.FromResult(0) : action(o.Value);
        }

        /// <summary>
        ///     Throws exception if <c>value</c> is not null.
        /// </summary>
        public static T Null<T>(this T value, string message = null) where T : class
        {
            if (value != null)
            {
                throw new ArgumentException(
                    message ?? "Value of type '{0}' is not null".Fmt(typeof(T).Name));
            }

            return null;
        }

        /// <summary>
        ///     Throws exception if <c>value</c> is null.
        /// </summary>
        public static T NotNull<T>(this T value, string message = null) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(
                    message ?? "Value of type '{0}' is null".Fmt(typeof(T).Name));
            }

            return value;
        }

        /// <summary>
        ///     Throws exception if <c>value</c> is null.
        /// </summary>
        public static T NotNull<T>(this T? value, string message = null) where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(
                    message ?? "Value of type '{0}' is null".Fmt(typeof(T).Name));
            }

            return value.Value;
        }

        /// <summary>
        ///     Throws exception if <c>value</c> is false.
        /// </summary>
        public static void IsTrue(this bool value, string message = null)
        {
            if (!value)
            {
                throw new ArgumentException(message ?? "Value is not true");
            }
        }

        /// <summary>
        ///     Throws exception if <c>value</c> is true.
        /// </summary>
        public static void IsFalse(this bool value, string message = null)
        {
            if (value)
            {
                throw new ArgumentException(message ?? "Value is not false");
            }
        }

        /// <summary>
        ///     Throws exception
        /// </summary>
        public static void Fail(string message = null)
        {
            throw new InvalidOperationException(message);
        }

        /// <summary>
        ///     Executes <c>action1</c> if <c>source</c> is of type <c>TInput1</c>.
        ///     Returns <c>source</c>.
        /// </summary>
        public static TInputBase DoTypeSwitch<TInputBase, TInput1>(
            this TInputBase source,
            Action<TInput1> action1)
            where TInputBase : class
        {
            return source.DoTypeSwitch(action1, _ => { });
        }

        /// <summary>
        ///     Executes <c>action1</c> if <c>source</c> is of type <c>TInput1</c>.
        ///     Otherwise <c>actionBase</c> is executed.
        ///     Returns <c>source</c>.
        /// </summary>
        public static TInputBase DoTypeSwitch<TInputBase, TInput1>(
            this TInputBase source,
            Action<TInput1> action1,
            Action<TInputBase> actionBase)
            where TInputBase : class
        {
            action1.NotNull();
            actionBase.NotNull();

            if (source is TInput1)
            {
                action1((TInput1)(object)source);
                return source;
            }

            actionBase(source);
            return source;
        }

        /// <summary>
        ///     Executes <c>action1</c> if <c>source</c> is of type <c>TInput1</c>.
        ///     Otherwise executes <c>action2</c> if <c>source</c> is of type <c>TInput2</c>.
        ///     Otherwise <c>actionBase</c> is executed.
        ///     Returns <c>source</c>.
        /// </summary>
        public static TInputBase DoTypeSwitch<TInputBase, TInput1, TInput2>(
            this TInputBase source,
            Action<TInput1> action1,
            Action<TInput2> action2,
            Action<TInputBase> actionBase)
            where TInputBase : class
        {
            return source.DoTypeSwitch(action1, inputBase => inputBase.DoTypeSwitch(action2, actionBase));
        }

        /// <summary>
        ///     Executes <c>action1</c> if <c>source</c> is of type <c>TInput1</c>.
        ///     Otherwise executes <c>action2</c> if <c>source</c> is of type <c>TInput2</c>.
        ///     Otherwise executes <c>action3</c> if <c>source</c> is of type <c>TInput3</c>.
        ///     Otherwise <c>actionBase</c> is executed.
        ///     Returns <c>source</c>.
        /// </summary>
        public static TInputBase DoTypeSwitch<TInputBase, TInput1, TInput2, TInput3>(
            this TInputBase source,
            Action<TInput1> action1,
            Action<TInput2> action2,
            Action<TInput3> action3,
            Action<TInputBase> actionBase)
            where TInputBase : class
        {
            return source.DoTypeSwitch(action1, action2, inputBase => inputBase.DoTypeSwitch(action3, actionBase));
        }

        /// <summary>
        ///     Executes <c>action1</c> if <c>source</c> is of type <c>TInput1</c>.
        ///     Otherwise executes <c>action2</c> if <c>source</c> is of type <c>TInput2</c>.
        ///     Otherwise executes <c>action3</c> if <c>source</c> is of type <c>TInput3</c>.
        ///     Otherwise executes <c>action4</c> if <c>source</c> is of type <c>TInput4</c>.
        ///     Otherwise <c>actionBase</c> is executed.
        ///     Returns <c>source</c>.
        /// </summary>
        public static TInputBase DoTypeSwitch<TInputBase, TInput1, TInput2, TInput3, TInput4>(
            this TInputBase source,
            Action<TInput1> action1,
            Action<TInput2> action2,
            Action<TInput3> action3,
            Action<TInput4> action4,
            Action<TInputBase> actionBase)
            where TInputBase : class
        {
            return source.DoTypeSwitch(
                action1,
                action2,
                action3,
                inputBase => inputBase.DoTypeSwitch(action4, actionBase));
        }

        /// <summary>
        ///     Executes <c>action1</c> if <c>source</c> is of type <c>TInput1</c>.
        ///     Otherwise executes <c>action2</c> if <c>source</c> is of type <c>TInput2</c>.
        ///     Otherwise executes <c>action3</c> if <c>source</c> is of type <c>TInput3</c>.
        ///     Otherwise executes <c>action4</c> if <c>source</c> is of type <c>TInput4</c>.
        ///     Otherwise executes <c>action5</c> if <c>source</c> is of type <c>TInput5</c>.
        ///     Otherwise <c>actionBase</c> is executed.
        ///     Returns <c>source</c>.
        /// </summary>
        public static TInputBase DoTypeSwitch<TInputBase, TInput1, TInput2, TInput3, TInput4, TInput5>(
            this TInputBase source,
            Action<TInput1> action1,
            Action<TInput2> action2,
            Action<TInput3> action3,
            Action<TInput4> action4,
            Action<TInput5> action5,
            Action<TInputBase> actionBase)
            where TInputBase : class
        {
            return source.DoTypeSwitch(
                action1,
                action2,
                action3,
                action4,
                inputBase => inputBase.DoTypeSwitch(action5, actionBase));
        }

        /// <summary>
        ///     Executes <c>func1</c> if <c>source</c> is of type <c>TInput1</c> and returns result of it.
        ///     Otherwise <c>failureValue</c> is returned.
        /// </summary>
        public static TResult TypeSwitch<TInputBase, TInput1, TResult>(
            this TInputBase source,
            Func<TInput1, TResult> func1,
            TResult failureValue = default(TResult))
            where TInputBase : class
        {
            return source.TypeSwitch(func1, _ => failureValue);
        }

        /// <summary>
        ///     Executes <c>func1</c> if <c>source</c> is of type <c>TInput1</c> and returns result of it.
        ///     Otherwise <c>funcBase</c> is executed and result of it is returned.
        /// </summary>
        public static TResult TypeSwitch<TInputBase, TInput1, TResult>(
            this TInputBase source,
            Func<TInput1, TResult> func1,
            Func<TInputBase, TResult> funcBase)
            where TInputBase : class
        {
            func1.NotNull();
            funcBase.NotNull();

            if (source is TInput1)
            {
                return func1((TInput1)(object)source);
            }

            return funcBase(source);
        }

        /// <summary>
        ///     Executes <c>func1</c> if <c>source</c> is of type <c>TInput1</c> and returns result of it.
        ///     Otherwise executes <c>func2</c> if <c>source</c> is of type <c>TInput2</c> and returns result of it.
        ///     Otherwise <c>funcBase</c> is executed and result of it is returned.
        /// </summary>
        public static TResult TypeSwitch<TInputBase, TInput1, TInput2, TResult>(
            this TInputBase source,
            Func<TInput1, TResult> func1,
            Func<TInput2, TResult> func2,
            Func<TInputBase, TResult> funcBase)
            where TInputBase : class
        {
            return source.TypeSwitch(func1, inputBase => inputBase.TypeSwitch(func2, funcBase));
        }

        /// <summary>
        ///     Executes <c>func1</c> if <c>source</c> is of type <c>TInput1</c> and returns result of it.
        ///     Otherwise executes <c>func2</c> if <c>source</c> is of type <c>TInput2</c> and returns result of it.
        ///     Otherwise executes <c>func3</c> if <c>source</c> is of type <c>TInput3</c> and returns result of it.
        ///     Otherwise <c>funcBase</c> is executed and result of it is returned.
        /// </summary>
        public static TResult TypeSwitch<TInputBase, TInput1, TInput2, TInput3, TResult>(
            this TInputBase source,
            Func<TInput1, TResult> func1,
            Func<TInput2, TResult> func2,
            Func<TInput3, TResult> func3,
            Func<TInputBase, TResult> funcBase)
            where TInputBase : class
        {
            return source.TypeSwitch(func1, func2, inputBase => inputBase.TypeSwitch(func3, funcBase));
        }

        /// <summary>
        ///     Executes <c>func1</c> if <c>source</c> is of type <c>TInput1</c> and returns result of it.
        ///     Otherwise executes <c>func2</c> if <c>source</c> is of type <c>TInput2</c> and returns result of it.
        ///     Otherwise executes <c>func3</c> if <c>source</c> is of type <c>TInput3</c> and returns result of it.
        ///     Otherwise executes <c>func4</c> if <c>source</c> is of type <c>TInput4</c> and returns result of it.
        ///     Otherwise <c>funcBase</c> is executed and result of it is returned.
        /// </summary>
        public static TResult TypeSwitch<TInputBase, TInput1, TInput2, TInput3, TInput4, TResult>(
            this TInputBase source,
            Func<TInput1, TResult> func1,
            Func<TInput2, TResult> func2,
            Func<TInput3, TResult> func3,
            Func<TInput4, TResult> func4,
            Func<TInputBase, TResult> funcBase)
            where TInputBase : class
        {
            return source.TypeSwitch(func1, func2, func3, inputBase => inputBase.TypeSwitch(func4, funcBase));
        }

        /// <summary>
        ///     Executes <c>func1</c> if <c>source</c> is of type <c>TInput1</c> and returns result of it.
        ///     Otherwise executes <c>func2</c> if <c>source</c> is of type <c>TInput2</c> and returns result of it.
        ///     Otherwise executes <c>func3</c> if <c>source</c> is of type <c>TInput3</c> and returns result of it.
        ///     Otherwise executes <c>func4</c> if <c>source</c> is of type <c>TInput4</c> and returns result of it.
        ///     Otherwise executes <c>func5</c> if <c>source</c> is of type <c>TInput5</c> and returns result of it.
        ///     Otherwise <c>funcBase</c> is executed and result of it is returned.
        /// </summary>
        public static TResult TypeSwitch<TInputBase, TInput1, TInput2, TInput3, TInput4, TInput5, TResult>(
            this TInputBase source,
            Func<TInput1, TResult> func1,
            Func<TInput2, TResult> func2,
            Func<TInput3, TResult> func3,
            Func<TInput4, TResult> func4,
            Func<TInput5, TResult> func5,
            Func<TInputBase, TResult> funcBase)
            where TInputBase : class
        {
            return source.TypeSwitch(func1, func2, func3, func4, inputBase => inputBase.TypeSwitch(func5, funcBase));
        }
    }
}

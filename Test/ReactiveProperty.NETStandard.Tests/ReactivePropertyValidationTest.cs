﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using ReactiveProperty.Tests.Strings;

namespace ReactiveProperty.Tests
{
    [TestClass]
    public class ReactivePropertyValidationTest
    {
        private TestTarget target;

        [TestInitialize]
        public void Initialize()
        {
            target = new TestTarget();
        }

        [TestCleanup]
        public void Cleanup()
        {
            target = null;
        }

        [TestMethod]
        public void InitialState()
        {
            target.RequiredProperty.HasErrors.IsTrue();
        }

        [TestMethod]
        public void AnnotationTest()
        {
            var errors = new List<IEnumerable>();
            target.RequiredProperty
                .ObserveErrorChanged
                .Where(x => x != null)
                .Subscribe(errors.Add);
            errors.Count.Is(1);
            errors[0].Cast<string>().Is("error!");
            target.RequiredProperty.HasErrors.IsTrue();

            target.RequiredProperty.Value = "a";
            errors.Count.Is(1);
            target.RequiredProperty.HasErrors.IsFalse();

            target.RequiredProperty.Value = null;
            errors.Count.Is(2);
            errors[1].Cast<string>().Is("error!");
            target.RequiredProperty.HasErrors.IsTrue();
        }

        [TestMethod]
        public void BothTest()
        {
            IEnumerable error = null;
            target.BothProperty
                .ObserveErrorChanged
                .Subscribe(x => error = x);

            target.BothProperty.HasErrors.IsTrue();
            error.OfType<string>().Is("required");

            target.BothProperty.Value = "a";
            target.BothProperty.HasErrors.IsFalse();
            error.IsNull();

            target.BothProperty.Value = "aaaaaa";
            target.BothProperty.HasErrors.IsTrue();
            error.IsNotNull();
            error.Cast<string>().Is("5over");

            target.BothProperty.Value = null;
            target.BothProperty.HasErrors.IsTrue();
            error.Cast<string>().Is("required");
        }

        [TestMethod]
        public void TaskTest()
        {
            var errors = new List<IEnumerable>();
            target.TaskValidationTestProperty
                .ObserveErrorChanged
                .Where(x => x != null)
                .Subscribe(errors.Add);
            errors.Count.Is(1);
            errors[0].OfType<string>().Is("required");

            target.TaskValidationTestProperty.Value = "a";
            target.TaskValidationTestProperty.HasErrors.IsFalse();
            errors.Count.Is(1);

            target.TaskValidationTestProperty.Value = null;
            target.TaskValidationTestProperty.HasErrors.IsTrue();
            errors.Count.Is(2);
        }

        [TestMethod]
        public async Task AsyncValidation_SuccessCase()
        {
            var tcs = new TaskCompletionSource<string>();
            var rprop = new ReactiveProperty<string>().SetValidateNotifyError(_ => tcs.Task);

            IEnumerable error = null;
            rprop.ObserveErrorChanged.Subscribe(x => error = x);

            rprop.HasErrors.IsFalse();
            error.IsNull();

            rprop.Value = "dummy";  //--- push value
            tcs.SetResult(null);    //--- validation success!
            await Task.Yield();

            rprop.HasErrors.IsFalse();
            error.IsNull();
        }

        [TestMethod]
        public async Task AsyncValidation_FailedCase()
        {
            var tcs = new TaskCompletionSource<string>();
            var rprop = new ReactiveProperty<string>().SetValidateNotifyError(_ => tcs.Task);

            IEnumerable error = null;
            rprop.ObserveErrorChanged.Subscribe(x => error = x);

            rprop.HasErrors.IsFalse();
            error.IsNull();

            var errorMessage = "error occured!!";
            rprop.Value = "dummy";  //--- push value
            tcs.SetResult(errorMessage);    //--- validation error!
            await Task.Delay(10);

            rprop.HasErrors.IsTrue();
            error.IsNotNull();
            error.Cast<string>().Is(errorMessage);
            rprop.GetErrors("Value").Cast<string>().Is(errorMessage);
        }

        [TestMethod]
        public void AsyncValidation_ThrottleTest()
        {
            var scheduler = new TestScheduler();
            var rprop = new ReactiveProperty<string>()
                            .SetValidateNotifyError(xs =>
                            {
                                return xs
                                        .Throttle(TimeSpan.FromSeconds(1), scheduler)
                                        .Select(x => string.IsNullOrEmpty(x) ? "required" : null);
                            });

            IEnumerable error = null;
            rprop.ObserveErrorChanged.Subscribe(x => error = x);

            scheduler.AdvanceTo(TimeSpan.FromMilliseconds(0).Ticks);
            rprop.Value = string.Empty;
            rprop.HasErrors.IsFalse();
            error.IsNull();

            scheduler.AdvanceTo(TimeSpan.FromMilliseconds(300).Ticks);
            rprop.Value = "a";
            rprop.HasErrors.IsFalse();
            error.IsNull();

            scheduler.AdvanceTo(TimeSpan.FromMilliseconds(700).Ticks);
            rprop.Value = "b";
            rprop.HasErrors.IsFalse();
            error.IsNull();

            scheduler.AdvanceTo(TimeSpan.FromMilliseconds(1100).Ticks);
            rprop.Value = string.Empty;
            rprop.HasErrors.IsFalse();
            error.IsNull();

            scheduler.AdvanceTo(TimeSpan.FromMilliseconds(2500).Ticks);
            rprop.HasErrors.IsTrue();
            error.IsNotNull();
            error.Cast<string>().Is("required");
        }

        [TestMethod]
        public void ErrorChangedNonePattern()
        {
            var errors = new List<IEnumerable>();
            var rprop = new ReactiveProperty<string>()
                .SetValidateNotifyError(x => string.IsNullOrWhiteSpace(x) ? "error" : null);
            // old version behavior
            rprop.ObserveErrorChanged.Skip(1).Subscribe(errors.Add);

            errors.Count.Is(0);

            rprop.Value = "OK";
            errors.Count.Is(1);
            errors.Last().IsNull();

            rprop.Value = null;
            errors.Count.Is(2);
            errors.Last().OfType<string>().Is("error");
        }

        [TestMethod]
        public void IgnoreInitErrorAndForceNotify()
        {
            var rp = new ReactiveProperty<string>(mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
                .SetValidateNotifyError(x => string.IsNullOrEmpty(x) ? "error" : null);

            rp.HasErrors.IsFalse();
            rp.ForceNotify();
            rp.HasErrors.IsTrue();
        }

        [TestMethod]
        public void IgnoreInitErrorAndForceValidate()
        {
            var rp = new ReactiveProperty<string>(mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
                .SetValidateNotifyError(x => string.IsNullOrEmpty(x) ? "error" : null);

            rp.HasErrors.IsFalse();
            rp.ForceValidate();
            rp.HasErrors.IsTrue();
        }

        [TestMethod]
        public void IgnoreInitErrorAndUpdateValue()
        {
            var rp = new ReactiveProperty<string>(mode: ReactivePropertyMode.Default | ReactivePropertyMode.IgnoreInitialValidationError)
                .SetValidateNotifyError(x => string.IsNullOrEmpty(x) ? "error" : null);

            rp.HasErrors.IsFalse();
            rp.Value = "";
            rp.HasErrors.IsTrue();
        }

        [TestMethod]
        public void CustomValidationErrorMessage()
        {
            var target = new TestTarget();
            target.CustomValidationErrorMessageProperty.Value = "";
            var errorMessage = target
                .CustomValidationErrorMessageProperty
                .GetErrors(nameof(TestTarget.CustomValidationErrorMessageProperty))
                .Cast<string>()
                .First();
            errorMessage.Is("Custom validation error message for CustomValidationErrorMessageProperty");
        }

        [TestMethod]
        public void CustomValidationErrorMessageWithDisplayName()
        {
            var target = new TestTarget();
            target.CustomValidationErrorMessageWithDisplayNameProperty.Value = "";
            var errorMessage = target
                .CustomValidationErrorMessageWithDisplayNameProperty
                .GetErrors(nameof(TestTarget.CustomValidationErrorMessageWithDisplayNameProperty))
                .Cast<string>()
                .First();
            errorMessage.Is("Custom validation error message for CustomName");
        }

        [TestMethod]
        public void CustomValidationErrorMessageWithResource()
        {
            var target = new TestTarget();
            target.CustomValidationErrorMessageWithResourceProperty.Value = "";
            var errorMessage = target
                .CustomValidationErrorMessageWithResourceProperty
                .GetErrors(nameof(TestTarget.CustomValidationErrorMessageWithResourceProperty))
                .Cast<string>()
                .First();
            errorMessage.Is("Oops!? FromResource is required.");
        }
    }

    internal class TestTarget
    {
        [Required(ErrorMessage = "error!")]
        public ReactiveProperty<string> RequiredProperty { get; private set; }

        [StringLength(5, ErrorMessage = "5over")]
        public ReactiveProperty<string> BothProperty { get; private set; }

        public ReactiveProperty<string> TaskValidationTestProperty { get; private set; }

        [Required(ErrorMessage = "Custom validation error message for {0}")]
        public ReactiveProperty<string> CustomValidationErrorMessageProperty { get; }

        [Required(ErrorMessage = "Custom validation error message for {0}")]
        [Display(Name = "CustomName")]
        public ReactiveProperty<string> CustomValidationErrorMessageWithDisplayNameProperty { get; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ValidationErrorMessage))]
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.ValidationTargetPropertyName))]
        public ReactiveProperty<string> CustomValidationErrorMessageWithResourceProperty { get; }

        public TestTarget()
        {
            RequiredProperty = new ReactiveProperty<string>()
                .SetValidateAttribute(() => RequiredProperty);

            BothProperty = new ReactiveProperty<string>()
                .SetValidateAttribute(() => BothProperty)
                .SetValidateNotifyError(s => string.IsNullOrWhiteSpace(s) ? "required" : null);

            TaskValidationTestProperty = new ReactiveProperty<string>()
                .SetValidateNotifyError(async s =>
                {
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        return await Task.FromResult("required");
                    }
                    return await Task.FromResult((string)null);
                });

            CustomValidationErrorMessageProperty = new ReactiveProperty<string>()
                .SetValidateAttribute(() => CustomValidationErrorMessageProperty);

            CustomValidationErrorMessageWithDisplayNameProperty = new ReactiveProperty<string>()
                .SetValidateAttribute(() => CustomValidationErrorMessageWithDisplayNameProperty);

            CustomValidationErrorMessageWithResourceProperty = new ReactiveProperty<string>()
                .SetValidateAttribute(() => CustomValidationErrorMessageWithResourceProperty);
        }
    }
}

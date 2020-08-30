﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;

namespace ReactiveProperty.Tests
{
    [TestClass]
    public class ReactivePropertySlimTest
    {
        [TestMethod]
        public void NormalCase()
        {
            var rp = new ReactivePropertySlim<string>();
            rp.Value.IsNull();
            rp.Subscribe(x => x.IsNull());
        }

        [TestMethod]
        public void InitialValue()
        {
            var rp = new ReactivePropertySlim<string>("Hello world");
            rp.Value.Is("Hello world");
            rp.Subscribe(x => x.Is("Hello world"));
        }

        [TestMethod]
        public void NoRaiseLatestValueOnSubscribe()
        {
            var rp = new ReactivePropertySlim<string>(mode: ReactivePropertyMode.DistinctUntilChanged);
            var called = false;
            rp.Subscribe(_ => called = true);
            called.Is(false);
        }

        [TestMethod]
        public void NoDistinctUntilChanged()
        {
            var rp = new ReactivePropertySlim<string>(mode: ReactivePropertyMode.RaiseLatestValueOnSubscribe);
            var list = new List<string>();
            rp.Subscribe(list.Add);
            rp.Value = "Hello world";
            rp.Value = "Hello world";
            rp.Value = "Hello japan";
            list.Is(null, "Hello world", "Hello world", "Hello japan");
        }

        private class IgnoreCaseComparer : EqualityComparer<string>
        {
            public override bool Equals(string x, string y)
                => x?.ToLower() == y?.ToLower();

            public override int GetHashCode(string obj)
                => (obj?.ToLower()).GetHashCode();
        }

        [TestMethod]
        public void CustomEqualityComparer()
        {
            var rp = new ReactivePropertySlim<string>(equalityComparer: new IgnoreCaseComparer());
            var list = new List<string>();
            rp.Subscribe(list.Add);
            rp.Value = "Hello world";
            rp.Value = "HELLO WORLD";
            rp.Value = "Hello japan";
            list.Is(null, "Hello world", "Hello japan");
        }

        [TestMethod]
        public void EnumCase()
        {
            var rp = new ReactivePropertySlim<TestEnum>();
            var results = new List<TestEnum>();
            rp.Subscribe(results.Add);
            results.Is(TestEnum.None);

            rp.Value = TestEnum.Enum1;
            results.Is(TestEnum.None, TestEnum.Enum1);

            rp.Value = TestEnum.Enum2;
            results.Is(TestEnum.None, TestEnum.Enum1, TestEnum.Enum2);
        }

        [TestMethod]
        public void ForceNotify()
        {
            var rp = new ReactivePropertySlim<int>(0);
            var collector = new List<int>();
            rp.Subscribe(collector.Add);

            collector.Is(0);
            rp.ForceNotify();
            collector.Is(0, 0);
        }

        [TestMethod]
        public void UnsubscribeTest()
        {
            var rp = new ReactivePropertySlim<int>(mode: ReactivePropertyMode.None);
            var collector = new List<(string, int)>();
            var a = rp.Select(x => ("a", x)).Subscribe(collector.Add);
            var b = rp.Select(x => ("b", x)).Subscribe(collector.Add);
            var c = rp.Select(x => ("c", x)).Subscribe(collector.Add);

            rp.Value = 99;
            collector.Is(("a", 99), ("b", 99), ("c", 99));

            collector.Clear();
            a.Dispose();

            rp.Value = 40;
            collector.Is(("b", 40), ("c", 40));

            collector.Clear();
            c.Dispose();

            rp.Value = 50;
            collector.Is(("b", 50));

            collector.Clear();
            b.Dispose();

            rp.Value = 9999;
            collector.Count.Is(0);

            var d = rp.Select(x => ("d", x)).Subscribe(collector.Add);

            rp.Value = 9;
            collector.Is(("d", 9));

            rp.Dispose();
        }
    }
}

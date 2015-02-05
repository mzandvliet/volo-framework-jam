﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RamjetAnvil.StateMachine {

    public static class ReflectionUtils {
        public static IList<MethodInfo> GetMethodsWithAttribute(Type type, Type attributeType, BindingFlags flags) {
            var methods = type.GetMethods(flags);
            var stateMethods = new List<MethodInfo>();
            foreach (var m in methods) {
                var attributes = m.GetCustomAttributes(attributeType, false);
                if (attributes.Length > 0) {
                    stateMethods.Add(m);
                }
            }
            return stateMethods;
        }

        /// <summary>
        /// Builds a Delegate instance from the supplied MethodInfo object and a target to invoke against.
        /// </summary>
        public static Delegate ToDelegate(MethodInfo methodInfo, object target) {
            if (methodInfo == null) throw new ArgumentNullException("methodInfo");

            Type delegateType;

            var typeArgs = methodInfo.GetParameters()
                .Select(p => p.ParameterType)
                .ToList();

            // builds a delegate type
            if (methodInfo.ReturnType == typeof (void)) {
                delegateType = Expression.GetActionType(typeArgs.ToArray());
            }
            else {
                typeArgs.Add(methodInfo.ReturnType);
                delegateType = Expression.GetFuncType(typeArgs.ToArray());
            }

            // creates a binded delegate if target is supplied
            var result = (target == null)
                ? Delegate.CreateDelegate(delegateType, methodInfo)
                : Delegate.CreateDelegate(delegateType, target, methodInfo);

            return result;
        }
    }

    public class LinkedList<T> {
        private LinkedListNode<T> _first;

        public LinkedListNode<T> First {
            get { return _first; }
        }

        public void Add(T item) {
            var node = new LinkedListNode<T>(item);

            if (_first != null) {
                node.Next = _first;
                _first.Previous = node;
            }
            _first = node;
        }

        public void Remove(LinkedListNode<T> item) {
            if (_first == item) {
                _first = item.Next;
            }
            else {
                if (item.Next != null) {
                    item.Previous.Next = item.Next;
                    item.Next.Previous = item.Previous;
                }
                else if (item.Previous != null) {
                    item.Previous.Next = null;
                }
            }
            item.Previous = null;
            item.Next = null;
        }
    }

    public class LinkedListNode<T> {
        public LinkedListNode<T> Previous;
        public LinkedListNode<T> Next;

        public T Value { get; private set; }

        public LinkedListNode(T value) {
            Value = value;
        }
    }

    public class IteratableStack<T> {
        private IList<T> _stack;

        public int Count {
            get { return _stack.Count; }
        }

        public IteratableStack() {
            _stack = new List<T>();
        }

        public IteratableStack(int capacity) {
            _stack = new List<T>(capacity);
        }

        public IteratableStack(IEnumerable<T> collection) {
            _stack = new List<T>(collection);
        }

        public void Clear() {
            _stack.Clear();
        }

        public bool Contains(T item) {
            return _stack.Contains(item);
        }

        public T Peek() {
            return _stack[LastIndex()];
        }

        public T Pop() {
            var removed = _stack[LastIndex()];
            _stack.RemoveAt(LastIndex());
            return removed;
        }

        public void Push(T item) {
            _stack.Add(item);
        }

        public T this[int i] {
            get { return _stack[i]; }
        }

        private int LastIndex() {
            return _stack.Count - 1;
        }
    }
}
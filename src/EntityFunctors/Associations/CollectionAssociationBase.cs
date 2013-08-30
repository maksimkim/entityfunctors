namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;
    using Fluent;

    public abstract class CollectionAssociationBase<TSource, TSourceItem, TTarget, TTargetItem> 
        : IMappingAssociation, ICollectionAssociation, IExpandable
        where TSource : class
        where TTarget : class, new()
    {
        private static readonly MethodInfo ToArray;

        private static readonly MethodInfo Select;

        public string Key { get; private set; }

        public MappingDirection Direction
        {
            get { return MappingDirection.Read; }
        }

        public LambdaExpression Source { get; private set; }

        public LambdaExpression Target { get; private set; }

        public Type SourceItemType
        {
            get { return typeof(TSourceItem); }
        }

        public Type TargetItemType 
        {
            get { return typeof(TTargetItem); }
        }

        public bool Expand { get; private set; }

        static CollectionAssociationBase()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<int>>> exp = xs => xs.Select(x => x).ToArray();

            ToArray = ((MethodCallExpression)exp.Body).Method.GetGenericMethodDefinition();

            Select = ((MethodCallExpression)((MethodCallExpression)exp.Body).Arguments[0]).Method.GetGenericMethodDefinition();
        }

        protected CollectionAssociationBase(
            Expression<Func<TSource, IEnumerable<TSourceItem>>> source,
            Expression<Func<TTarget, IEnumerable<TTargetItem>>> target
        )
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);

            PropertyInfo prop;
            Contract.Assert(source.Body.TryGetProperty(out prop));
            Contract.Assert(target.Body.TryGetProperty(out prop));

            Source = source;
            Target = target;

            Key = Target.GetProperty().GetName();
        }

        public virtual Expression BuildMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys, IMappingRegistry registry)
        {
            if (!(@from.Type == typeof(TSource) && to.Type == typeof(TTarget)))
                return Expression.Empty();

            var donor = Source.Apply(@from);
            var acceptor = Target.Apply(to);

            Expression mapper = Expression.Assign(
                acceptor,
                Expression.Condition(
                    donor.CreateCheckForDefault(),
                    Target.ReturnType.GetDefaultExpression(),
                    Expression.Convert(
                        Expression.Call(
                            ToArray.MakeGenericMethod(TargetItemType),
                            Expression.Call(
                                Select.MakeGenericMethod(SourceItemType, TargetItemType),
                                donor,
                                CreateSelector(SourceItemType, TargetItemType, propertyKeys, registry)
                            )
                        ),
                        Target.ReturnType
                    )
                )
            );

            if (propertyKeys != null && Expand)
                mapper =
                    Expression.IfThen(
                        propertyKeys.CreateContains(Expression.Constant(Key, typeof(string))),
                        mapper
                    );

            return mapper;
        }

        protected abstract LambdaExpression CreateSelector(Type @from, Type to, ParameterExpression expands, IMappingRegistry registry);

        public void Expandable()
        {
            Expand = true;
        }
    }
}
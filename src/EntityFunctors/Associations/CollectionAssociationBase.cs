namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;

    public abstract class CollectionAssociationBase<TSource, TTarget> : IExpandable, IMappingAssociation
    {
        private static readonly MethodInfo ToArray;

        private static readonly MethodInfo Select;

        public PropertyPart Source { get; private set; }

        public PropertyPart Target { get; private set; }

        public MappingDirection Direction
        {
            get { return MappingDirection.Read; }
        }

        protected string Expand { get; set; }

        static CollectionAssociationBase()
        {
            Expression<Func<IEnumerable<int>, IEnumerable<int>>> exp = xs => xs.Select(x => x).ToArray();

            ToArray = ((MethodCallExpression)exp.Body).Method.GetGenericMethodDefinition();

            Select = ((MethodCallExpression)((MethodCallExpression)exp.Body).Arguments[0]).Method.GetGenericMethodDefinition();
        }

        protected CollectionAssociationBase(PropertyPart source, PropertyPart target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);

            var propTarget = target.Property;

            if (!(propTarget.PropertyType.IsGenericType && propTarget.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                throw new InvalidOperationException(
                    string.Format(
                        "Collection mapping supports only IEnumerable<T> type for target property {0} but found {1}",
                        typeof(TTarget).Name + "." + propTarget.Name,
                        propTarget.PropertyType
                        )
                    );

            Target = target;

            Source = source;
        }

        public virtual Expression BuildMapper(ParameterExpression @from, ParameterExpression to, IMappingRegistry registry, ParameterExpression expands)
        {
            if (!(@from.Type == typeof(TSource) && to.Type == typeof(TTarget)))
                return Expression.Empty();

            var propFrom = Expression.Property(@from, Source.Property);
            var propTo = Expression.Property(to, Target.Property);

            var itemTypeFrom = Source.Property.PropertyType.GetItemType();
            var itemTypeTo = Target.Property.PropertyType.GetItemType();

            Expression mapper = Expression.Assign(
                propTo,
                Expression.Condition(
                    propFrom.CreateCheckForDefault(),
                    Target.Property.PropertyType.GetDefaultExpression(),
                    Expression.Convert(
                        Expression.Call(
                            ToArray.MakeGenericMethod(itemTypeTo),
                            Expression.Call(
                                Select.MakeGenericMethod(itemTypeFrom, itemTypeTo),
                                propFrom,
                                CreateSelector(itemTypeFrom, itemTypeTo, expands, registry)
                            )
                        ),
                        Target.Property.PropertyType
                    )
                )
            );

            if (expands != null && !string.IsNullOrWhiteSpace(Expand))
                mapper =
                    Expression.IfThen(
                        expands.CreateContains(Expression.Constant(Expand, typeof(string))),
                        mapper
                    );

            return mapper;
        }

        public PropertyInfo TargetProperty
        {
            get { return Target.Property; }
        }

        public Expression Rewrite(Expression original, ParameterExpression parameter)
        {
            return Expression.Property(parameter, Source.Property);
        }

        public IEnumerable<KeyValuePair<PropertyInfo, Delegate>> ValueConverters
        {
            get { yield break; }
        }

        protected abstract LambdaExpression CreateSelector(Type @from, Type to, ParameterExpression expands, IMappingRegistry registry);

        public void Expandable()
        {
            Expand = Target.Property.GetContractName();
        }
    }
}
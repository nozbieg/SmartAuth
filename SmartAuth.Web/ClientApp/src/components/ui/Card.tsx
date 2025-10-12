import React from "react";

export type CardTag = 'section' | 'div' | 'article' | 'aside';

interface BaseCardProps {
  title?: string;
  subtitle?: string;
  footer?: React.ReactNode;
  headingLevel?: 1 | 2 | 3 | 4 | 5 | 6;
  className?: string;
}

// Polimorficzne propsy: łączymy własne + propsy elementu docelowego z pominięciem kolizji
export type CardProps<T extends CardTag = 'section'> = BaseCardProps & {
  as?: T;
} & Omit<React.ComponentPropsWithoutRef<T>, keyof BaseCardProps | 'as'>;

const defaultTag: CardTag = 'section';

export const Card = React.forwardRef<HTMLElement, CardProps<any>>(function CardInner<T extends CardTag = 'section'> (
  props: CardProps<T>, ref: React.Ref<HTMLElement>
) {
  const {
    as,
    title,
    subtitle,
    footer,
    headingLevel = 2,
    className = '',
    children,
    ...rest
  } = props as CardProps<CardTag>;

  const Comp: CardTag = as || defaultTag;
  const HeadingTag = (`h${headingLevel}` as keyof JSX.IntrinsicElements);

  return (
    <Comp
      ref={ref as any}
      className={"card" + (className ? ' ' + className : '')}
      {...rest}
    >
      {title && <HeadingTag>{title}</HeadingTag>}
      {subtitle && <p style={{ marginTop: '-.4rem' }}>{subtitle}</p>}
      {children}
      {footer && <div className="card-footer" style={{ marginTop: '.5rem' }}>{footer}</div>}
    </Comp>
  );
});

export default Card;

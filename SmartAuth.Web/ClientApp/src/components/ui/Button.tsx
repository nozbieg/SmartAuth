import React from "react";

export type ButtonVariant = "default" | "primary" | "outline" | "danger";
export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
}

const variantClass: Record<ButtonVariant, string> = {
  default: "btn",
  primary: "btn btn-primary",
  outline: "btn btn-outline",
  danger: "btn btn-danger",
};

export const Button: React.FC<ButtonProps> = ({ variant = "default", className = "", children, ...rest }) => {
  const cls = variantClass[variant] + (className ? " " + className : "");
  return (
    <button className={cls} {...rest}>{children}</button>
  );
};

export default Button;

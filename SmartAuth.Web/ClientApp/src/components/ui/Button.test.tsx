import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import Button from './Button';

describe('Button', () => {
  it('renderuje dzieci', () => {
    render(<Button>Kliknij</Button>);
    expect(screen.getByRole('button', { name: 'Kliknij' })).toBeInTheDocument();
  });
  it('dodaje klasę wariantu primary', () => {
    const { getByRole } = render(<Button variant="primary">OK</Button>);
    expect(getByRole('button').className).toMatch(/btn-primary/);
  });
  it('przekazuje disabled', () => {
    render(<Button disabled>Nieaktywny</Button>);
    expect(screen.getByRole('button')).toBeDisabled();
  });
});


import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import AuthLayout from './AuthLayout';

describe('AuthLayout', () => {
  it('renderuje domyślne tytuły i dzieci', () => {
    render(<AuthLayout><div data-testid="child">Child</div></AuthLayout>);
    expect(screen.getByRole('heading', { name: /SmartAuth/i })).toBeInTheDocument();
    expect(screen.getByTestId('child')).toBeInTheDocument();
  });
  it('nadpisuje mediaTitle i mediaDescription', () => {
    render(<AuthLayout mediaTitle="Tytuł" mediaDescription="Opis">X</AuthLayout>);
    expect(screen.getByRole('heading', { name: 'Tytuł' })).toBeInTheDocument();
    expect(screen.getByText('Opis')).toBeInTheDocument();
  });
  it('renderuje mediaHeader i mediaFooter', () => {
    render(<AuthLayout mediaHeader={<div data-testid="mh">H</div>} mediaFooter={<div data-testid="mf">F</div>}>X</AuthLayout>);
    expect(screen.getByTestId('mh')).toBeInTheDocument();
    expect(screen.getByTestId('mf')).toBeInTheDocument();
  });
});


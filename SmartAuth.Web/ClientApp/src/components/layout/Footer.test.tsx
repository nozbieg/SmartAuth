import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import Footer from './Footer';

describe('Footer', () => {
  it('renderuje bieżący rok domyślnie', () => {
    render(<Footer />);
    const year = new Date().getFullYear();
    expect(screen.getByText(new RegExp(year.toString()))).toBeInTheDocument();
  });
  it('renderuje własną notatkę', () => {
    render(<Footer note={<span>Custom note</span>} />);
    expect(screen.getByText(/Custom note/i)).toBeInTheDocument();
  });
});


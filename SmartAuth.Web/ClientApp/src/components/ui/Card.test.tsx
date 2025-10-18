import { render, screen } from '@testing-library/react';
import Card from './Card';

describe('Card', () => {
    it('renderuje tytuł, podtytuł i footer', () => {
        render(<Card title="Tytul" subtitle="Podtytul" footer={<span data-testid="f">Stopka</span>}>Treść</Card>);
        expect(screen.getByRole('heading', { name: 'Tytul' })).toBeInTheDocument();
        expect(screen.getByText('Podtytul')).toBeInTheDocument();
        expect(screen.getByText('Treść')).toBeInTheDocument();
        expect(screen.getByTestId('f')).toBeInTheDocument();
    });

    it('poziom nagłówka zgodny z headingLevel', () => {
        const { container } = render(<Card title="Nag" headingLevel={3}>X</Card>);
        const h3 = container.querySelector('h3');
        expect(h3).not.toBeNull();
        expect(h3?.textContent).toBe('Nag');
    });

    it('polimorficzny as=article', () => {
        const { container } = render(<Card as="article" title="A">Zawartość</Card>);
        const article = container.querySelector('article.card');
        expect(article).not.toBeNull();
    });

    it('nie renderuje nagłówka gdy brak title', () => {
        const { container } = render(<Card>Bez tytułu</Card>);
        expect(container.querySelector('h1,h2,h3,h4,h5,h6')).toBeNull();
    });
});

import {fireEvent, render, screen, waitFor} from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import {vi} from 'vitest';
import {MemoryRouter} from 'react-router-dom';
import RegisterPage from './RegisterPage';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual: any = await vi.importActual('react-router-dom');
    return {
        ...actual,
        useNavigate: () => mockNavigate
    };
});

function setup() {
    return render(
        <MemoryRouter initialEntries={[{pathname: '/register', state: {from: {pathname: '/home-prev'}}}]}>
            <RegisterPage/>
        </MemoryRouter>
    );
}

describe('RegisterPage', () => {
    beforeEach(() => {
        mockNavigate.mockReset();
        vi.useRealTimers();
    });

    it('renderuje podstawowe pola formularza', () => {
        setup();
        expect(screen.getByLabelText(/Email/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/Imię i nazwisko/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/Hasło \(min. 8 znaków\)/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/Powtórz hasło/i)).toBeInTheDocument();
        expect(screen.getByRole('button', {name: /Utwórz konto/i})).toBeInTheDocument();
    });

    it('pokazuje błąd gdy nie zaakceptowano regulaminu', async () => {
        setup();
        await userEvent.type(screen.getByLabelText(/Email/i), 'john@example.com');
        await userEvent.type(screen.getByLabelText(/Imię i nazwisko/i), 'John Doe');
        await userEvent.type(screen.getByLabelText(/Hasło \(min. 8 znaków\)/i), '12345678');
        await userEvent.type(screen.getByLabelText(/Powtórz hasło/i), '12345678');
        fireEvent.submit(screen.getByRole('button', {name: /Utwórz konto/i}).closest('form')!);
        expect(await screen.findByText(/Musisz zaakceptować regulamin/i)).toBeInTheDocument();
    });

    it('pokazuje błąd gdy hasło za krótkie', async () => {
        setup();
        await userEvent.type(screen.getByLabelText(/Email/i), 'john@example.com');
        await userEvent.type(screen.getByLabelText(/Imię i nazwisko/i), 'John Doe');
        await userEvent.click(screen.getByRole('checkbox'));
        await userEvent.type(screen.getByLabelText(/Hasło \(min. 8 znaków\)/i), '1234567');
        await userEvent.type(screen.getByLabelText(/Powtórz hasło/i), '1234567');
        fireEvent.submit(screen.getByRole('button', {name: /Utwórz konto/i}).closest('form')!);
        expect(await screen.findByText(/Hasło musi mieć co najmniej 8 znaków/i)).toBeInTheDocument();
    });

    it('pokazuje błąd gdy hasła się różnią', async () => {
        setup();
        await userEvent.type(screen.getByLabelText(/Email/i), 'john@example.com');
        await userEvent.type(screen.getByLabelText(/Imię i nazwisko/i), 'John Doe');
        await userEvent.click(screen.getByRole('checkbox'));
        await userEvent.type(screen.getByLabelText(/Hasło \(min. 8 znaków\)/i), '12345678');
        await userEvent.type(screen.getByLabelText(/Powtórz hasło/i), '87654321');
        fireEvent.submit(screen.getByRole('button', {name: /Utwórz konto/i}).closest('form')!);
        expect(await screen.findByText(/Hasła nie są identyczne/i)).toBeInTheDocument();
    });

    it('wyświetla błąd z serwera przy nieudanej rejestracji', async () => {
        (globalThis as any).fetch = vi.fn().mockResolvedValue({ok: false, text: () => Promise.resolve('Serwer padł')});
        setup();
        await userEvent.type(screen.getByLabelText(/Email/i), 'john@example.com');
        await userEvent.type(screen.getByLabelText(/Imię i nazwisko/i), 'John Doe');
        await userEvent.click(screen.getByRole('checkbox'));
        await userEvent.type(screen.getByLabelText(/Hasło \(min. 8 znaków\)/i), '12345678');
        await userEvent.type(screen.getByLabelText(/Powtórz hasło/i), '12345678');
        fireEvent.submit(screen.getByRole('button', {name: /Utwórz konto/i}).closest('form')!);
        expect(await screen.findByText(/Serwer padł/i)).toBeInTheDocument();
        expect(global.fetch).toHaveBeenCalledWith('/api/auth/register', expect.any(Object));
    });

    it('wysyła formularz i przekierowuje po sukcesie', async () => {
        const fetchMock = vi.fn().mockResolvedValue({ok: true, text: () => Promise.resolve('')});
        (globalThis as any).fetch = fetchMock;

        setup();
        await userEvent.type(screen.getByLabelText(/Email/i), ' john@example.com ');
        await userEvent.type(screen.getByLabelText(/Imię i nazwisko/i), ' John Doe ');
        await userEvent.click(screen.getByRole('checkbox'));
        await userEvent.type(screen.getByLabelText(/Hasło \(min. 8 znaków\)/i), '12345678');
        await userEvent.type(screen.getByLabelText(/Powtórz hasło/i), '12345678');

        fireEvent.submit(screen.getByRole('button', {name: /Utwórz konto/i}).closest('form')!);

        await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
        const fetchArgs = fetchMock.mock.calls[0];
        expect(fetchArgs[0]).toBe('/api/auth/register');
        const body = JSON.parse(fetchArgs[1].body);
        expect(body.email).toBe('john@example.com');
        expect(body.displayName).toBe('John Doe');

        expect(await screen.findByText(/Konto utworzone/i)).toBeInTheDocument();

        await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith('/login', {
            replace: true,
            state: {from: '/home-prev'}
        }));
    }, 10000);
});

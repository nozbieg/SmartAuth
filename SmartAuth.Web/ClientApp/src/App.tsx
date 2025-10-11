import { useEffect, useState } from 'react'

export default function App() {
    const [msg, setMsg] = useState('Loading...')
    useEffect(() => {
        fetch('/api/hello')
            .then(r => r.json())
            .then(d => setMsg(d.message ?? JSON.stringify(d)))
            .catch(e => setMsg('Error: ' + e))
    }, [])
    return (
        <div style={{ fontFamily: 'sans-serif', padding: 24 }}>
            <h1>Hello World (React)</h1>
            <p>Backend says: <b>{msg}</b></p>
        </div>
    )
}

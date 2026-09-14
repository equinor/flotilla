import App from '../src/App'
import { test } from 'vitest'
import { createRoot } from 'react-dom/client'

test('renders without crashing', () => {
    const div = document.createElement('div')
    createRoot(((<App />), div))
})

import { createGlobalStyle } from 'styled-components'

export const GlobalStyle = createGlobalStyle`
    *,
    *::before,
    *::after {
        box-sizing: border-box;
    }
    body {
        margin: 0;
    }
    /* Pages render the bars and their body as siblings of #root. */
    #root {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
    }
`

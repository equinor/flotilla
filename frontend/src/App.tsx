import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { SignInPage } from './components/SignInPage/SignInPage'
import { FlotillaSite } from 'components/FrontPage/FlotillaSite'
import styled from 'styled-components'

const StyledApp = styled.div`
    margin: 2rem;
`

function App() {
    return (
        <StyledApp>
            <UnauthenticatedTemplate>
                <div className="sign-in-page">
                    <SignInPage></SignInPage>
                </div>
            </UnauthenticatedTemplate>
            <AuthenticatedTemplate>
                <FlotillaSite />
            </AuthenticatedTemplate>
        </StyledApp>
    )
}

export default App

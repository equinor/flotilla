import { useIsAuthenticated } from '@azure/msal-react'
import { useMsal } from '@azure/msal-react'
import { loginRequest } from '../../../api/AuthConfig'
import { Button } from '@equinor/eds-core-react'
import { IPublicClientApplication } from '@azure/msal-browser'
import styled from 'styled-components'

const Centered = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
`

function handleLogin(instance: IPublicClientApplication) {
    instance.loginRedirect(loginRequest).catch((e) => {
        console.error(e)
    })
}

/**
 * Renders a button which, when selected, will redirect the page to the login prompt
 */
export const SignInButton = () => {
    const { instance } = useMsal()

    return (
        <Centered>
            <Button href="" variant="contained" onClick={() => handleLogin(instance)}>
                Sign in
            </Button>
        </Centered>
    )
}

export const SignInPage = () => {
    const isAuthenticated = useIsAuthenticated()

    return (
        <>
            <div className="sign-in-button">{isAuthenticated ? <span>Signed In</span> : <SignInButton />}</div>
        </>
    )
}

import { useNavigate } from 'react-router-dom'
import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import notfound from 'mediaAssets/404notfound.png'
import { config } from 'config'
import { StyledPage } from 'components/Styles/StyledComponents'
import { Header } from 'components/Header/Header'
import { phone_width } from 'utils/constants'

const StyledPageContent = styled.div`
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    display: flex;
    flex-direction: row;
    align-items: center;

    @media (max-width: ${phone_width}) {
        flex-direction: column;
        top: 60%;
        left: 50%;
        transform: translate(-50%, -50%);
    }
`
const StyledTypography = styled(Typography)`
    text-align: center;
    gap: 10px;
`
const StyledImage = styled.img`
    height: 500px;
    padding: 0px 10px;

    @media (max-width: ${phone_width}) {
        max-width: 50vw;
        height: auto;
        padding: 5px;
    }
`
const StyledActions = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 0px 10px;
    gap: 20px;
`
const StyledButton = styled(Button)`
    width: 200px;
    justify-content: center;
`

export const PageNotFound = () => {
    const navigate = useNavigate()
    return (
        <>
            <Header page={'404'} />
            <StyledPage>
                <StyledPageContent>
                    <StyledImage src={notfound} />
                    <StyledActions>
                        <StyledTypography variant="h3">
                            {"We couldn't find the page you're looking for."}
                        </StyledTypography>
                        <StyledButton
                            color="secondary"
                            onClick={() => {
                                navigate(`${config.FRONTEND_BASE_ROUTE}/front-page`)
                            }}
                        >
                            {"Let's go back"}
                        </StyledButton>
                    </StyledActions>
                </StyledPageContent>
            </StyledPage>
        </>
    )
}

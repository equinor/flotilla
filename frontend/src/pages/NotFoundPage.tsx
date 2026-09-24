import { useNavigate } from 'react-router'
import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import notfound from 'mediaAssets/404notfound.png'
import { PageContent, PageBackground } from 'components/Styles/StyledComponents'
import { Header } from 'components/Header/Header'
import { phone_width } from 'utils/constants'

const NotFoundContent = styled(PageContent)`
    flex: 1;
    flex-direction: row;
    align-items: center;
    justify-content: center;

    @media (max-width: ${phone_width}) {
        flex-direction: column;
    }
`
const StyledTypography = styled(Typography)`
    text-align: center;
`
const StyledImage = styled.img`
    height: 500px;
    padding: 0px 10px;

    @media (max-width: ${phone_width}) {
        max-width: 100%;
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
            <Header />
            <PageBackground>
                <NotFoundContent>
                    <StyledImage src={notfound} />
                    <StyledActions>
                        <StyledTypography variant="h3">
                            {"We couldn't find the page you're looking for."}
                        </StyledTypography>
                        <StyledButton color="secondary" onClick={() => navigate(`/`)}>
                            {"Let's go back"}
                        </StyledButton>
                    </StyledActions>
                </NotFoundContent>
            </PageBackground>
        </>
    )
}

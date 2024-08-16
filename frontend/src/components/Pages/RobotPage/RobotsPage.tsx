import { RobotStatusSection } from 'components/Pages/FrontPage/RobotCards/RobotStatusSection'
import { Header } from 'components/Header/Header'
import styled from 'styled-components'
import { StyledPage } from 'components/Styles/StyledComponents'

const StyledRobotsPage = styled(StyledPage)`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100%, 1fr));
    gap: 3rem;
`

export const RobotsPage = () => {
    return (
        <>
            <Header page={'robotsPage'} />
            <StyledRobotsPage>
                <RobotStatusSection />
            </StyledRobotsPage>
        </>
    )
}

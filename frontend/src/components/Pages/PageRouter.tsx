import { useParams } from 'react-router-dom'
import { MissionPage } from './MissionPage/MissionPage'
import { RobotPage } from './RobotPage/RobotPage'
import { MissionDefinitionPage } from './MissionDefinitionPage/MissionDefinitionPage'
import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'

const StyledTypography = styled.div`
    position: absolute;
    top: 30%;
    left: 50%;
    transform: translate(-70%, -50%);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 10px;
`
export const PageRouter = () => {
    const { page } = useParams()
    if (!page) return InvalidRoute()

    const [pageName, id] = page.split(/-(.+)/)

    if (ValidateUUID(id)) return PageNotFound()

    switch (pageName) {
        case 'mission':
            return <MissionPage missionId={id} />
        case 'missiondefinition':
            return <MissionDefinitionPage missionId={id} />
        case 'robot':
            return <RobotPage robotId={id} />
        default:
            return PageNotFound()
    }
}
const ValidateUUID = (id: string) => {
    return (id.match(/-/g) || []).length > 4
}

const PageNotFound = () => {
    return (
        <StyledTypography>
            <Typography variant="h1"> {'404'} </Typography>
            <Typography variant="h2">{'Page Not Found'}</Typography>
            <Typography variant="body_short">{"We could't find the page you're looking for."} </Typography>
        </StyledTypography>
    )
}

const InvalidRoute = () => {
    return (
        <StyledTypography>
            <Typography variant="body_short">Invalid route</Typography>
        </StyledTypography>
    )
}

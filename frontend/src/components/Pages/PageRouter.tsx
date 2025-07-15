import { useParams } from 'react-router-dom'
import { MissionPage } from './MissionPage/MissionPage'
import { RobotPage } from './RobotPage/RobotPage'
import { MissionDefinitionPage } from './MissionDefinitionPage/MissionDefinitionPage'
import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { PageNotFound } from './NotFoundPage'

const StyledTypography = styled(Typography)`
    text-align: center;
    gap: 10px;
`

export const PageRouter = () => {
    const { page } = useParams()
    if (!page) return InvalidRoute()

    const [pageName, id] = page.split(/-(.+)/)

    if (!ValidateUUID(id)) return PageNotFound()

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
    const regex = /^[\da-f]{8}-([\da-f]{4}-){3}[\da-f]{12}$/i
    return regex.test(id)
}

const InvalidRoute = () => {
    return <StyledTypography variant="body_short">Invalid route</StyledTypography>
}

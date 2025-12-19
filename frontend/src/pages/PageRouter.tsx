import { useNavigate, useSearchParams } from 'react-router-dom'
import { MissionPage, SimpleMissionPage } from './MissionPage/MissionPage'
import { RobotPage } from './RobotPage/RobotPage'
import { MissionDefinitionPage } from './MissionDefinitionPage/MissionDefinitionPage'
import { config } from 'config'
import { useEffect, useState } from 'react'

enum PageRouterPrefixes {
    Mission = 'mission',
    MissionDefinition = 'missiondefinition',
    Robot = 'robot',
    SimpleMission = 'mission-simple',
}

interface PageRouterProps {
    prefix: PageRouterPrefixes
}

const PageRouter = ({ prefix }: PageRouterProps) => {
    const navigate = useNavigate()
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()
    const [id, setId] = useState<string | undefined>(undefined)

    useEffect(() => {
        if (!searchParams || searchParams.size < 1) {
            navigate(`${config.FRONTEND_BASE_ROUTE}/page-not-found`)
            return
        }

        const id = searchParams.get('id')
        if (!id || !ValidateUUID(id)) {
            navigate(`${config.FRONTEND_BASE_ROUTE}/page-not-found`)
            return
        }
        setId(id)
    }, [searchParams])

    if (!id) return <></>

    switch (prefix) {
        case PageRouterPrefixes.SimpleMission:
            return <SimpleMissionPage missionId={id} />
        case PageRouterPrefixes.Mission:
            return <MissionPage missionId={id} />
        case PageRouterPrefixes.MissionDefinition:
            return <MissionDefinitionPage missionId={id} />
        case PageRouterPrefixes.Robot:
            return <RobotPage robotId={id} />
        default:
            return <></>
    }
}

export const SimpleMissionPageRouter = () => {
    return <PageRouter prefix={PageRouterPrefixes.SimpleMission} />
}

export const MissionPageRouter = () => {
    return <PageRouter prefix={PageRouterPrefixes.Mission} />
}

export const MissionDefinitionPageRouter = () => {
    return <PageRouter prefix={PageRouterPrefixes.MissionDefinition} />
}

export const RobotPageRouter = () => {
    return <PageRouter prefix={PageRouterPrefixes.Robot} />
}

const ValidateUUID = (id: string) => {
    const regex = /^[\da-f]{8}-([\da-f]{4}-){3}[\da-f]{12}$/i
    return regex.test(id)
}

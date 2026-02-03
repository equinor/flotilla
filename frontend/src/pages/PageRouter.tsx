import { useNavigate, useSearchParams } from 'react-router-dom'
import { MissionPage, SimpleMissionPage } from './MissionPage/MissionPage'
import { RobotPage } from './RobotPage/RobotPage'
import { MissionDefinitionPage } from './MissionDefinitionPage/MissionDefinitionPage'
import { config } from 'config'
import { useEffect, useState } from 'react'
import { useAssetContext } from 'components/Contexts/AssetContext'

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
    const [inspectionId, setInspectionId] = useState<string | undefined>(undefined)
    const [analysisId, setAnalysisId] = useState<string | undefined>(undefined)
    const { enabledRobots } = useAssetContext()
    const selectedRobot = enabledRobots.find((robot) => robot.id === id)

    useEffect(() => {
        if (!searchParams || searchParams.size < 1) {
            navigate(`${config.FRONTEND_BASE_ROUTE}/page-not-found`)
            return
        }

        const id = searchParams.get('id')
        const inspectionId = searchParams.get('inspectionId')
        const analysisId = searchParams.get('analysisId')
        if (!id || !ValidateUUID(id)) {
            navigate(`${config.FRONTEND_BASE_ROUTE}/page-not-found`)
            return
        }
        if (inspectionId) {
            if (!ValidateUUID(inspectionId)) {
                navigate(`${config.FRONTEND_BASE_ROUTE}/page-not-found`)
                return
            }
            setInspectionId(inspectionId)
        } else {
            setInspectionId(undefined)
        }
        if (analysisId) {
            if (!ValidateUUID(analysisId)) {
                navigate(`${config.FRONTEND_BASE_ROUTE}/page-not-found`)
                return
            }
            setAnalysisId(analysisId)
        } else {
            setAnalysisId(undefined)
        }
        setId(id)
    }, [searchParams])

    if (!id) return <></>

    switch (prefix) {
        case PageRouterPrefixes.SimpleMission:
            return <SimpleMissionPage missionId={id} inspectionId={inspectionId} analysisId={analysisId} />
        case PageRouterPrefixes.Mission:
            return <MissionPage missionId={id} inspectionId={inspectionId} analysisId={analysisId} />
        case PageRouterPrefixes.MissionDefinition:
            return <MissionDefinitionPage missionId={id} />
        case PageRouterPrefixes.Robot:
            if (!selectedRobot) {
                return <>Loading robot...</>
            }
            return <RobotPage robot={selectedRobot} />
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

import { TaskTable } from 'components/Pages/MissionPage/TaskOverview/TaskTable'
import { VideoStreamWindow } from 'components/Pages/MissionPage/VideoStream/VideoStreamWindow'
import { Mission } from 'models/Mission'
import { VideoStream } from 'models/VideoStream'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import styled from 'styled-components'
import { MissionDefinitionHeader } from './MissionHeader/MissionDefinitionHeader'
import { BackButton } from '../../../utils/BackButton'
import { BackendAPICaller } from 'api/ApiCaller'

import { Header } from 'components/Header/Header'
import { MissionDefinition } from 'models/MissionDefinition'

const StyledMissionDefinitionPage = styled.div`
    display: flex;
    flex-wrap: wrap;
    justify-content: start;
    flex-direction: column;
    gap: 1rem;
    margin: 2rem;
`

export function MissionDefinitionPage() {
    const { missionId } = useParams()
    const [selectedMissionDefinition, setSelectedMissionDefinition] = useState<MissionDefinition>()

    useEffect(() => {
        if (missionId) {
            BackendAPICaller.getMissionDefinitionById(missionId).then((mission) => {
                setSelectedMissionDefinition(mission)
            })
        }
    }, [missionId])

    useEffect(() => {
        const timeDelay = 1000
        const id = setInterval(() => {
            if (missionId) {
                BackendAPICaller.getMissionDefinitionById(missionId).then((mission) => {
                    setSelectedMissionDefinition(mission)
                })
            }
        }, timeDelay)
        return () => clearInterval(id)
    }, [missionId])

    return (
        <>
            <Header page={'mission'} />
            <StyledMissionDefinitionPage>
                <BackButton />
                {selectedMissionDefinition !== undefined && (
                    <>
                        <MissionDefinitionHeader missionDefinition={selectedMissionDefinition} />
                    </>
                )}
            </StyledMissionDefinitionPage>
        </>
    )
}

import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { TaskTable } from './TaskOverview/TaskTable'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionMapView } from './MapPosition/MissionMapView'
import { Mission } from 'models/Mission'
import { phone_width } from 'utils/constants'

const TaskAndMapSection = styled.div`
    display: flex;
    min-height: 60%;
    padding: 24px;
    @media (max-width: ${phone_width}) {
        padding: 6px 8px 8px 6px;
    }
    @media (min-width: ${phone_width}) {
        min-width: 930px;
    }
    flex-direction: column;
    justify-content: center;
    align-items: flex-start;
    gap: 8px;
    align-self: stretch;
    border-radius: 6px;
    border: 1px solid ${tokens.colors.ui.background__medium.rgba};
    background: ${tokens.colors.ui.background__default.rgba};
`

const StyledTableAndMap = styled.div`
    display: flex;
    flex-wrap: wrap;
    align-items: top;
    gap: 30px;
`

interface TaskTableAndMapProps {
    mission: Mission
    missionDefinitionPage: boolean
}

export const TaskTableAndMap = ({ mission, missionDefinitionPage }: TaskTableAndMapProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <TaskAndMapSection>
            <Typography variant="h4">{TranslateText('Tasks')}</Typography>
            <StyledTableAndMap>
                <TaskTable tasks={mission?.tasks} missionDefinitionPage={missionDefinitionPage} />
                {mission.missionId && <MissionMapView mission={mission} />}
            </StyledTableAndMap>
        </TaskAndMapSection>
    )
}

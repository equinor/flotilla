import styled from 'styled-components'
import { tokens } from '@equinor/eds-tokens'
import { RobotWithoutTelemetry } from 'models/Robot'
import { DashboardRobotInfo } from './DashboardRobotInfo'
import { DashboardMissionStatus } from './DashboardMissionStatus'

const DashboardRobotCardStyle = styled.div`
    display: flex;
    flex-direction: column;
    margin-inline: calc(var(--eds-page-space-horizontal) * -1);
    padding-inline: var(--eds-page-space-horizontal);
    border-left: 5px solid ${tokens.colors.interactive.primary__resting.hex};
    border-bottom: 1px dashed ${tokens.colors.ui.background__medium.hex};
    padding-bottom: 16px;

    &:last-child {
        border-bottom: none;
        padding-bottom: 0;
    }
`

const RobotAndMissionRow = styled.div`
    display: flex;
    align-items: flex-start;

    @media (max-width: 960px) {
        flex-direction: column;
    }
`

export const DashboardRobotStatusCard = ({ robot }: { robot: RobotWithoutTelemetry }) => {
    return (
        <DashboardRobotCardStyle>
            <RobotAndMissionRow>
                <DashboardRobotInfo robot={robot} />
                <DashboardMissionStatus robot={robot} />
            </RobotAndMissionRow>
        </DashboardRobotCardStyle>
    )
}

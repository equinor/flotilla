import { useInspectionsContext } from 'components/Contexts/InpectionsContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import {
    StyledImageCard,
    StyledImagesSection,
    StyledInspectionCards,
    StyledInspectionContent,
    StyledInspectionData,
    StyledSection,
} from './InspectionStyles'
import { Typography } from '@equinor/eds-core-react'
import { GetInspectionImage } from './InspectionReportUtilities'
import { formatDateTime } from 'utils/StringFormatting'

const InspectionOverview = ({ tasks }: { tasks: Task[] }) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionTask } = useInspectionsContext()

    return (
        <StyledImagesSection>
            <StyledInspectionCards>
                {Object.keys(tasks).length > 0 &&
                    tasks.map(
                        (task) =>
                            task.status === TaskStatus.Successful && (
                                <StyledImageCard
                                    key={task.isarTaskId}
                                    onClick={() => switchSelectedInspectionTask(task)}
                                >
                                    <GetInspectionImage task={task} />
                                    <StyledInspectionData>
                                        {task.tagId && (
                                            <StyledInspectionContent>
                                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                                <Typography variant="body_short">{task.tagId}</Typography>
                                            </StyledInspectionContent>
                                        )}
                                        {task.endTime && (
                                            <StyledInspectionContent>
                                                <Typography variant="caption">
                                                    {TranslateText('Timestamp') + ':'}
                                                </Typography>
                                                <Typography variant="body_short">
                                                    {formatDateTime(task.endTime!, 'dd.MM.yy - HH:mm')}
                                                </Typography>
                                            </StyledInspectionContent>
                                        )}
                                    </StyledInspectionData>
                                </StyledImageCard>
                            )
                    )}
            </StyledInspectionCards>
        </StyledImagesSection>
    )
}

export const InspectionOverviewSection = ({ tasks }: { tasks: Task[] }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledSection
            style={{
                width: 'auto',
                borderColor: 'auto',
                padding: 'auto',
                maxHeight: 'auto',
            }}
        >
            <Typography variant="h4">{TranslateText('Last completed inspection')}</Typography>
            <InspectionOverview tasks={tasks} />
        </StyledSection>
    )
}

export const InspectionOverviewDialogView = ({ tasks }: { tasks: Task[] }) => {
    return (
        <StyledSection
            style={{
                width: '350px',
                borderColor: 'white',
                padding: '0px',
                maxHeight: '60vh',
            }}
        >
            <InspectionOverview tasks={tasks} />
        </StyledSection>
    )
}

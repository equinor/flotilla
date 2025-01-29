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

interface InspectionsOverviewProps {
    tasks: Task[]
    dialogView?: boolean | undefined
}

export const InspectionOverview = ({ tasks, dialogView }: InspectionsOverviewProps) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionTask } = useInspectionsContext()

    return (
        <StyledSection
            style={{
                width: dialogView ? '350px' : 'auto',
                borderColor: dialogView ? 'white' : 'auto',
                padding: dialogView ? '0px' : 'auto',
                maxHeight: dialogView ? '60vh' : 'auto',
            }}
        >
            {!dialogView && <Typography variant="h4">{TranslateText('Last completed inspection')}</Typography>}
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
                                                    <Typography variant="caption">
                                                        {TranslateText('Tag') + ':'}
                                                    </Typography>
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
        </StyledSection>
    )
}

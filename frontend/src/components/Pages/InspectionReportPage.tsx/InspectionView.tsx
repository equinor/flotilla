import { Icon, Typography } from '@equinor/eds-core-react'
import { Task, TaskStatus } from 'models/Task'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import { useInspectionsContext } from 'components/Contexts/InpectionsContext'
import {
    StyledBottomContent,
    StyledCloseButton,
    StyledDialog,
    StyledDialogContent,
    StyledDialogHeader,
    StyledDialogInspectionView,
    StyledImageCard,
    StyledImagesSection,
    StyledInfoContent,
    StyledInspection,
    StyledInspectionCards,
    StyledInspectionContent,
    StyledInspectionData,
    StyledInspectionImage,
    StyledSection,
} from './InspectionStyles'
import { BackendAPICaller } from 'api/ApiCaller'
import { useQuery } from '@tanstack/react-query'

interface InspectionDialogViewProps {
    task: Task
    tasks: Task[]
}

export const InspectionDialogView = ({ task, tasks }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationName } = useInstallationContext()
    const { switchSelectedInspectionTask } = useInspectionsContext()
    const { data } = FetchImageData(task)

    return (
        <>
            {data !== undefined && (
                <StyledDialog open={true}>
                    <StyledDialogContent>
                        <StyledDialogHeader>
                            <Typography variant="accordion_header" group="ui">
                                {TranslateText('Inspection report')}
                            </Typography>
                            <StyledCloseButton variant="ghost" onClick={() => switchSelectedInspectionTask(undefined)}>
                                <Icon name={Icons.Clear} size={24} />
                            </StyledCloseButton>
                        </StyledDialogHeader>
                        <StyledDialogInspectionView>
                            <div>
                                {data !== undefined && <StyledInspection src={data} />}
                                <StyledBottomContent>
                                    <StyledInfoContent>
                                        <Typography variant="caption">{TranslateText('Installation') + ':'}</Typography>
                                        <Typography variant="body_short">{installationName}</Typography>
                                    </StyledInfoContent>
                                    <StyledInfoContent>
                                        <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                        <Typography variant="body_short">{task.tagId}</Typography>
                                    </StyledInfoContent>
                                    {task.description && (
                                        <StyledInfoContent>
                                            <Typography variant="caption">
                                                {TranslateText('Description') + ':'}
                                            </Typography>
                                            <Typography variant="body_short">{task.description}</Typography>
                                        </StyledInfoContent>
                                    )}
                                    {task.endTime && (
                                        <StyledInfoContent>
                                            <Typography variant="caption">
                                                {TranslateText('Timestamp') + ':'}
                                            </Typography>
                                            <Typography variant="body_short">
                                                {formatDateTime(task.endTime, 'dd.MM.yy - HH:mm')}
                                            </Typography>
                                        </StyledInfoContent>
                                    )}
                                </StyledBottomContent>
                            </div>
                            <InspectionsViewSection tasks={tasks} dialogView={true} />
                        </StyledDialogInspectionView>
                    </StyledDialogContent>
                </StyledDialog>
            )}
        </>
    )
}

interface InspectionsViewSectionProps {
    tasks: Task[]
    dialogView?: boolean | undefined
}

export const InspectionsViewSection = ({ tasks, dialogView }: InspectionsViewSectionProps) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionTask } = useInspectionsContext()

    return (
        <>
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
        </>
    )
}

const FetchImageData = (task: Task) => {
    const { installationCode } = useInstallationContext()
    const data = useQuery({
        queryKey: [task.isarTaskId],
        queryFn: async () => {
            const imageBlob = await BackendAPICaller.getInspection(installationCode, task.isarTaskId!)
            return URL.createObjectURL(imageBlob)
        },
        retryDelay: 60 * 1000, // Will always wait 1 min to retry, regardless of how many retries
        staleTime: 10 * 60 * 1000, //  I don't want an API call for 10 min after the first time I get data
        enabled: task.status === TaskStatus.Successful && task.isarTaskId !== undefined,
    })
    return data
}

interface IGetInspectionImageProps {
    task: Task
}

const GetInspectionImage = ({ task }: IGetInspectionImageProps) => {
    const { data } = FetchImageData(task)
    return <>{data !== undefined && <StyledInspectionImage src={data} />}</>
}

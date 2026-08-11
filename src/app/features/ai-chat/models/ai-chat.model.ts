export interface ChatSessionListDto {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string | null;
  lastMessage: string | null;
  messageCount: number;
}

export interface ChatMessageDto {
  id: string;
  role: string;
  content: string;
  createdAt: string;
}

export interface ChatSessionDetailDto {
  id: string;
  title: string;
  createdAt: string;
  messages: ChatMessageDto[];
}

export interface CreateChatSessionDto {
  title?: string | null;
}

export interface SendChatMessageDto {
  message: string;
}

export interface ChatResponseDto {
  sessionId: string;
  userMessage: ChatMessageDto;
  assistantMessage: ChatMessageDto;
}

export interface UpdateSessionTitleDto {
  title: string;
}

export type ChatErrorKind =
  | 'rate-limit'
  | 'unauthorized'
  | 'forbidden'
  | 'not-found'
  | 'network'
  | 'timeout'
  | 'ai-unavailable'
  | 'validation'
  | 'server'
  | 'unknown';

export interface ChatErrorState {
  kind: ChatErrorKind;
  message: string;
}

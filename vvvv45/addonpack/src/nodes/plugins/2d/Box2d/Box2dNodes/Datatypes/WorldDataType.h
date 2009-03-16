#pragma once

namespace VVVV 
{
	namespace DataTypes 
	{
		[GuidAttribute("A1FFB623-3077-4435-A482-D87BC60A4443"),
		InterfaceType(ComInterfaceType::InterfaceIsIUnknown)]
		public interface class IBoxWorldIO: INodeIOBase
		{
			bool GetIsValid();
			void SetIsValid(bool value);
			b2World* GetWorld();
			void SetWorld(b2World* world);
			bool GetIsEnabled();
			void SetIsEnabled(bool value);
		};

		public ref class WorldDataType : IBoxWorldIO
		{
			private:
				static Guid^ FGuid;
				bool enabled;
				bool isvalid;
				b2World* mWorld;
				bool worldchanged;
				int bodycounter;
				int shapecounter;

			public:
				WorldDataType(void);
				
				virtual bool GetIsValid();
				virtual void SetIsValid(bool value);
				virtual bool GetIsEnabled();
				virtual void SetIsEnabled(bool value);
				virtual b2World* GetWorld();		
				virtual void SetWorld(b2World* world);

				int GetNewBodyId();
				int GetNewShapeId();

				bool Reset;
				vector<b2ContactPoint*>* Contacts;

				static String^ FriendlyName = "Box2d World";
				static property Guid^ GUID 
				{
					Guid^ get() 
					{
						if (FGuid == Guid::Empty) 
						{
							FGuid = gcnew System::Guid("A1FFB623-3077-4435-A482-D87BC60A4443");
						}
						return FGuid;
					}
				}



		};
	
	}
}



